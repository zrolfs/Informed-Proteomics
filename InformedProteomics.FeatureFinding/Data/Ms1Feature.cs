﻿using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;

namespace InformedProteomics.FeatureFinding.Data
{
    public class Ms1Feature
    {
        public Ms1Feature(int scanNum, double mass, int[] observedCharges, double abundance, ICollection<DeconvolutedPeak> peaks = null)
        {
            Mass = mass;
            Abundance = abundance;
            Charges = observedCharges;
            DeconvolutedPeaks = peaks;
            ScanNum = scanNum;
        }

        public readonly int ScanNum;
        public readonly int[] Charges;
        public double Mass { get; }
        public int MinCharge => Charges.Min();
        public int MaxCharge => Charges.Max();
        public double Abundance { get; }
        public ICollection<DeconvolutedPeak> DeconvolutedPeaks { get; }
    }
}
