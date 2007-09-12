/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2007 Dominik Schnitzer <dominik@schnitzer.at>
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */

    #define _ISOC9X_SOURCE  1
    #define _ISOC99_SOURCE  1

    #define __USE_ISOC9X    1
    #define __USE_ISOC99    1
# define M_PI       3.14159265358979323846


#include <math.h>
#include <string.h>
#include <time.h>

#include <fftw3.h>
#include <gst/gst.h>
#include <samplerate.h>

#include "gst-mirageaudio.h"

struct MirageAudio {

    // Gstreamer
    GstElement *pipeline;
    GstElement *audio;
    GstElement *src;
    GstElement *sink;

    gint rate;
    gint filerate;
    gint seconds;
    gint winsize;
    gint samples;

    // FFTW
    float *fftw;
    fftwf_plan fftwplan;
    gint fftwsamples;
    gint fftwsize;

    // libsamplerate
    SRC_STATE *src_state;
    SRC_DATA src_data;

    // Hann window
    float *window;

    // Output buffer
    float *out;
    gint hops;
    gint curhop;
    gint cursample;
    gboolean quit;
};

#define SRC_BUFFERLENGTH 4096

clock_t start;
clock_t end;


void tic()
{
    start = clock();
}

void toc()
{
    double ms;
    end = clock();
    ms = ((end - start)/(double)(CLOCKS_PER_SEC))*1000.0;
    g_print("libmirageaudio: time = %f\n", ms);
}

static void
mirageaudio_cb_newpad(GstElement *decodebin, GstPad *pad, gboolean last, MirageAudio *ma)
{
    GstCaps *caps;
    GstStructure *str;
    GstPad *audiopad;

    // only link once
    audiopad = gst_element_get_pad(ma->audio, "sink");
    if (GST_PAD_IS_LINKED(audiopad)) {
        g_object_unref(audiopad);
        return;
    }

    // check media type
    caps = gst_pad_get_caps(pad);
    str = gst_caps_get_structure(caps, 0);

    if (!g_strrstr(gst_structure_get_name(str), "audio")) {
        gst_caps_unref(caps);
        gst_object_unref(audiopad);
        return;
    }
    gst_caps_unref(caps);

    // link
    gst_pad_link(pad, audiopad);
}

static void
mirageaudio_cb_have_data(GstElement *element, GstBuffer *buffer, GstPad *pad, MirageAudio *ma)
{
    gint buffersamples;
    gint bufferpos;
    gint i;
    gint j;
    gint fill;

    // if data continues to flow/EOS is not yet processed
    if (ma->quit)
        return;

    // exit on empty buffer
    if (buffer->size <= 0)
        return;

    ma->src_data.data_in = (float*)GST_BUFFER_DATA(buffer);
    ma->src_data.input_frames = GST_BUFFER_SIZE(buffer)/sizeof(float);

    do {
        // set end of input flag if necessary
        ma->cursample += ma->src_data.input_frames;
        if (ma->cursample >= ma->seconds * ma->filerate) {
            ma->src_data.end_of_input = 1;
        }

        // resampling
        int err = src_process(ma->src_state, &ma->src_data);

        if (err != 0) {
            g_print("libmirageaudio: SRC Error - %s\n", src_strerror(err));
        }
        // return if no output
        if (ma->src_data.output_frames_gen == 0)
            return;

        buffersamples = ma->src_data.output_frames_gen;
        bufferpos = 0;

        // FFTW
        // If buffer does not get filled 
        if (ma->fftwsamples + buffersamples < ma->winsize) {
            memcpy(ma->fftw+ma->fftwsamples, ma->src_data.data_out, buffersamples*sizeof(float));
            ma->fftwsamples += buffersamples;

        // If buffer gets filled.
        } else {
            do {
                // prepare FFTW 
                fill = ma->winsize - ma->fftwsamples;

                if (fill <= 0)
                    g_print("libmirageaudio: Logic ERROR! fill <= 0\n");

                memcpy(ma->fftw+ma->fftwsamples, ma->src_data.data_out+bufferpos, fill*sizeof(float));
                memset(ma->fftw+ma->winsize, 0, ma->winsize*sizeof(float));
                for (i = 0; i < ma->winsize; i++) {
                    ma->fftw[i] = ma->fftw[i] * ma->window[i] * 32768.0f;
                }

                // Execute FFTW
                fftwf_execute(ma->fftwplan);

                // Powerspectrum
                ma->out[ma->curhop] = powf(ma->fftw[0], 2);
                for (j = 1; j < ma->winsize/2; j++) {
                    ma->out[j*ma->hops + ma->curhop] = powf(ma->fftw[j*2], 2) + powf(ma->fftw[ma->fftwsize-j*2], 2);
                }
                ma->out[(ma->winsize/2)*ma->hops + ma->curhop] = powf(ma->fftw[ma->winsize], 2);

                ma->fftwsamples = 0;
                buffersamples -= fill;
                bufferpos += fill;

                ma->curhop++;

                if (ma->curhop == ma->hops) {
                    GstBus *bus = gst_pipeline_get_bus(GST_PIPELINE(ma->pipeline));
                    gst_bus_post(bus, gst_message_new_eos(GST_OBJECT(ma->pipeline)));
                    g_print("libmirageaudio: EOS Message sent\n");
                    gst_object_unref(bus);
                    ma->quit = TRUE;
                    return;
                }

            } while (buffersamples >= ma->winsize);

            if (buffersamples > 0) {
                memcpy(ma->fftw, ma->src_data.data_out+bufferpos, buffersamples*sizeof(float));
                ma->fftwsamples = buffersamples;
            }
        }

        ma->src_data.data_in += ma->src_data.input_frames_used;
        ma->src_data.input_frames -= ma->src_data.input_frames_used;

    } while (ma->src_data.input_frames > 0);

    return;
}

MirageAudio*
mirageaudio_initialize(gint rate, gint seconds, gint winsize)
{
    MirageAudio *ma;
    gint i;

    ma = g_new0(MirageAudio, 1);
    ma->rate = rate;
    ma->seconds = seconds;
    ma->hops = rate*seconds/winsize;
    ma->out = malloc(ma->hops*((winsize/2)+1)*sizeof(float));

    // FFTW
    ma->winsize = winsize;
    ma->fftwsize = 2 * ma->winsize;
    ma->fftw = (float*)fftwf_malloc(ma->fftwsize * sizeof(float));
    ma->fftwplan = fftwf_plan_r2r_1d((2*ma->winsize), ma->fftw, ma->fftw, FFTW_R2HC,
                    FFTW_ESTIMATE | FFTW_DESTROY_INPUT);

    // Hann Window
    ma->window = malloc(ma->winsize*sizeof(float));
    for (i = 0; i < ma->winsize; i++) {
            ma->window[i] = (float)(0.5 * (1.0 - cos((2.0*M_PI *(double)i)/(double)(ma->winsize-1))));
    }

    // Samplingrate converter
    int error;
    ma->src_state = src_new(SRC_ZERO_ORDER_HOLD, 1, &error);
    ma->src_data.data_out = malloc(SRC_BUFFERLENGTH*sizeof(float));
    ma->src_data.output_frames = SRC_BUFFERLENGTH;

    return ma;
}

void
mirageaudio_initgstreamer(MirageAudio *ma, const gchar *file)
{
    GstPad *audiopad;
    GstCaps *filter_float;
    GstCaps *filter_resample;
    GstElement *cfilt_float;
    GstElement *cfilt_resample;
    GstElement *dec;
    GstElement *audioresample;
    GstElement *audioconvert;

    // Gstreamer decoder setup
    ma->pipeline = gst_pipeline_new("pipeline");

    // decoder
    ma->src = gst_element_factory_make("filesrc", "source");
    g_object_set(G_OBJECT(ma->src), "location", file, NULL);
    dec = gst_element_factory_make("decodebin", "decoder");
    g_signal_connect(dec, "new-decoded-pad", G_CALLBACK(mirageaudio_cb_newpad), ma);
    gst_bin_add_many(GST_BIN(ma->pipeline), ma->src, dec, NULL);
    gst_element_link(ma->src, dec);

    // audio conversion
    ma->audio = gst_bin_new("audio");

    audioconvert = gst_element_factory_make("audioconvert", "conv");

    filter_float = gst_caps_new_simple("audio/x-raw-float",
         "width", G_TYPE_INT, 32,
         NULL);
    cfilt_float = gst_element_factory_make("capsfilter", "cfilt_float");
    g_object_set(G_OBJECT(cfilt_float), "caps", filter_float, NULL);

    audioresample = gst_element_factory_make("audioresample", "resample");

    filter_resample =  gst_caps_new_simple("audio/x-raw-float",
          "channels", G_TYPE_INT, 1,
          NULL);
    cfilt_resample = gst_element_factory_make("capsfilter", "cfilt_resample");
    g_object_set(G_OBJECT(cfilt_resample), "caps", filter_resample, NULL);

    ma->sink = gst_element_factory_make("fakesink", "sink");
    g_object_set(G_OBJECT(ma->sink), "signal-handoffs", TRUE, NULL);
    g_signal_connect(ma->sink, "handoff", G_CALLBACK(mirageaudio_cb_have_data), ma);

    gst_bin_add_many(GST_BIN(ma->audio),
            audioconvert, audioresample,
            cfilt_resample, cfilt_float,
            ma->sink, NULL);
    gst_element_link_many(audioconvert, cfilt_float,
           audioresample, cfilt_resample,
           ma->sink, NULL);

    audiopad = gst_element_get_pad(audioconvert, "sink");
    gst_element_add_pad(ma->audio,
            gst_ghost_pad_new("sink", audiopad));
    gst_object_unref(audiopad);

    gst_bin_add(GST_BIN(ma->pipeline), ma->audio);

    // Get sampling rate of audio file
    if (gst_element_set_state(ma->pipeline, GST_STATE_PAUSED) == GST_STATE_CHANGE_ASYNC) {
        gst_element_get_state(ma->pipeline, NULL, NULL, GST_CLOCK_TIME_NONE);
    }
    GstPad *pad = gst_element_get_pad(ma->sink, "sink");
    GstCaps *caps = gst_pad_get_negotiated_caps(pad);
    GstStructure *str = gst_caps_get_structure(caps, 0);
    gst_structure_get_int(str, "rate", &ma->filerate);
    gst_object_unref(pad);
}

float*
mirageaudio_decode(MirageAudio *ma, const gchar *file, int *frames, int* size)
{
    GstBus *bus;
    gboolean decoding = TRUE;
    tic();

    ma->fftwsamples = 0;
    ma->curhop = 0;
    ma->cursample = 0;
    ma->quit = FALSE;

    // Gstreamer setup
    mirageaudio_initgstreamer(ma, file);

    // libsamplerate initialization
    ma->src_data.src_ratio = (double)ma->rate/(double)ma->filerate;
    ma->src_data.input_frames = 0;
    ma->src_data.end_of_input = 0;
    src_reset(ma->src_state);
    g_print("libmirageaudio: rate=%d, resampling=%f\n", ma->filerate, ma->src_data.src_ratio);

    // decode...
    gst_element_set_state(ma->pipeline, GST_STATE_PLAYING);
    g_print("libmirageaudio: decoding %s\n", file);
    bus = gst_pipeline_get_bus(GST_PIPELINE(ma->pipeline));
    while (decoding) {
        GstMessage* message = gst_bus_poll(bus, GST_MESSAGE_ANY, -1);
        if (message == NULL)
            continue;

        switch (GST_MESSAGE_TYPE(message)) {
            case GST_MESSAGE_ERROR: {
                GError *err;
                gchar *debug;

                gst_message_parse_error(message, &err, &debug);
                g_print("libmirageaudio: error: %s\n", err->message);
                g_error_free(err);
                g_free(debug);
                ma->curhop = 0;
                decoding = FALSE;

                break;
            }
            case GST_MESSAGE_EOS: {
                g_print("libmirageaudio: EOS Message received\n");
                decoding = FALSE;
                break;
            }
            default:
                break;
        }
    }
    gst_object_unref(bus);

    // Gstreamer cleanup
    gst_element_set_state(ma->pipeline, GST_STATE_NULL);
    gst_object_unref(GST_OBJECT(ma->pipeline));

    toc();

    *size = ma->winsize/2 + 1;
    *frames = ma->curhop;
    g_print("libmirageaudio: frames=%d (maxhops=%d), size=%d\n", *frames, ma->hops, *size);
    return ma->out;
}

MirageAudio*
mirageaudio_destroy(MirageAudio *ma)
{
    // FFTW cleanup
    fftwf_destroy_plan(ma->fftwplan);
    fftwf_free(ma->fftw);

    // Hann window clanup
    free(ma->window);

    // libsamplerate
    free(ma->src_data.data_out);
    src_delete(ma->src_state);

    // common
    free(ma->out);
    free(ma);

    return NULL;
}

void
mirageaudio_initgst()
{
    gst_init(NULL, NULL);
}
