// 
// NoNoiseSchemas.cs
// 
// Author:
//   Manuel Keglevic <manuel.keglevic@gmail.com>
//   Thomas Schulz <tjom@gmx.at>
// 
// Copyright (c) 2011 Manuel Keglevic, Thomas Schulz
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

using Mono.Addins;

using Banshee.Configuration;
using Banshee.Preferences;

namespace Banshee.NoNoise
{
    public static class NoNoiseSchemas
    {
        public enum PcaMfccOptions { Mean, SquaredMean, Median, Minimum, Maximum };

        internal static readonly SchemaEntry<bool> ShowNoNoise = new SchemaEntry<bool>(
            "nonoise", "show_nonoise",
            false,
            AddinManager.CurrentLocalizer.GetString ("NoNoise Visualization"),
            AddinManager.CurrentLocalizer.GetString ("Enable or disable the NoNoise Visualization")
        );
        
        internal static readonly SchemaEntry<string> PcaMfcc = new SchemaEntry<string>(
            "nonoise", "pca_mfcc",
            (string) Enum.GetName (typeof(PcaMfccOptions), PcaMfccOptions.Mean),
            AddinManager.CurrentLocalizer.GetString ("MFCC vector used"),
            AddinManager.CurrentLocalizer.GetString ("Selects which vector of the MFCC matrix should be used for the PCA")
        );

        internal static readonly SchemaEntry<bool> PcaUseDuration = new SchemaEntry<bool>(
            "nonoise", "pca_duration",
            true,
            AddinManager.CurrentLocalizer.GetString ("Song Duration"),
            AddinManager.CurrentLocalizer.GetString ("Use the song duration for the PCA")
        );
    }
}
