﻿using System;
using System.Collections.Generic;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Scoring.TopDown;

namespace InformedProteomics.TopDown.Scoring
{
    public class CompositeScorerBasedOnDeconvolutedSpectrum : CompositeScorer
    {
        // ReSharper disable SuggestBaseTypeForParameter
        public CompositeScorerBasedOnDeconvolutedSpectrum(DeconvolutedSpectrum deconvolutedSpectrum, ProductSpectrum spec, Tolerance productTolerance, IMassBinning comparer, ActivationMethod actMethod = ActivationMethod.Unknown)
            : this(deconvolutedSpectrum, GetRefIntensity(spec.Peaks), productTolerance, comparer, actMethod)
        // ReSharper restore SuggestBaseTypeForParameter
        {
        }

        public CompositeScorerBasedOnDeconvolutedSpectrum(DeconvolutedSpectrum deconvolutedSpectrum, double refPeakIntensity, Tolerance productTolerance, IMassBinning comparer, ActivationMethod actMethod = ActivationMethod.Unknown)
            : base(deconvolutedSpectrum, productTolerance, activationMethod: actMethod)
        {
            ReferencePeakIntensity = refPeakIntensity;
            _comparer = comparer;
            _massBinToPeakMap = new Dictionary<int, DeconvolutedPeak>();

            foreach (var p in deconvolutedSpectrum.Peaks)
            {
                var mass = p.Mz;
                var deltaMass = productTolerance.GetToleranceAsDa(mass, 1);
                var minMass = mass - deltaMass;
                var maxMass = mass + deltaMass;

                var binNum = comparer.GetBinNumber(mass);

                if (binNum < 0)
                {
                    binNum = comparer.GetBinNumber(minMass);
                    if (binNum < 0)
                    {
                        binNum = comparer.GetBinNumber(maxMass);
                    }
                }

                // filter out
                if (binNum < 0)
                {
                    continue;
                }

                UpdateDeconvPeak(binNum, p as DeconvolutedPeak);
                // going up
                for (var nextBinNum = binNum + 1; nextBinNum < comparer.NumberOfBins; nextBinNum++)
                {
                    var nextBinMass = comparer.GetMassStart(nextBinNum);
                    if (minMass < nextBinMass && nextBinMass < maxMass)
                    {
                        UpdateDeconvPeak(nextBinNum, p as DeconvolutedPeak); //_ionMassChkBins[nextBinNum] = true;
                    }
                    else
                    {
                        break;
                    }
                }

                // going down
                for (var prevBinNum = binNum - 1; prevBinNum < comparer.NumberOfBins; prevBinNum--)
                {
                    var prevBinMass = comparer.GetMassEnd(prevBinNum);
                    if (minMass < prevBinMass && prevBinMass < maxMass)
                    {
                        UpdateDeconvPeak(prevBinNum, p as DeconvolutedPeak); //_ionMassChkBins[prevBinNum] = true;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void UpdateDeconvPeak(int binNum, DeconvolutedPeak newPeak)
        {
            if (newPeak == null)
            {
                return;
            }

            if (_massBinToPeakMap.TryGetValue(binNum, out var existingPeak))
            {
                if (existingPeak.Intensity < newPeak.Intensity)
                {
                    _massBinToPeakMap[binNum] = newPeak;
                }
            }
            else
            {
                _massBinToPeakMap[binNum] = newPeak;
            }
        }

        public override double GetFragmentScore(Composition prefixFragmentComposition, Composition suffixFragmentComposition,
            AminoAcid nTerminalResidue = null,
            AminoAcid cTerminalResidue = null)
        {
            var score = 0.0;
            var prefixHit = false;
            var suffixHit = false;

            if (!InformedProteomics.Backend.Utils.FlipSwitch.UseFlipScoring)
            {
                foreach (var baseIonType in BaseIonTypes)
                {
                    var fragmentComposition = baseIonType.IsPrefix
                        ? prefixFragmentComposition + baseIonType.OffsetComposition
                        : suffixFragmentComposition + baseIonType.OffsetComposition;

                    var param = baseIonType.IsPrefix ? ScoreParam.Prefix : ScoreParam.Suffix;

                    var massBinNum = _comparer.GetBinNumber(fragmentComposition.Mass);
                    if (massBinNum >= 0 && massBinNum < _comparer.NumberOfBins)
                    {
                        if (_massBinToPeakMap.TryGetValue(massBinNum, out var existingPeak))
                        {
                            var massErrorPpm = 1e6 * (Math.Abs(existingPeak.Mass - fragmentComposition.Mass) / fragmentComposition.Mass);
                            score += param.Count;
                            score += param.Intensity * Math.Min(existingPeak.Intensity / ReferencePeakIntensity, 1.0); // intensity-based scoring
                            score += param.Dist * existingPeak.Dist; // Envelope distance-based scoring
                            score += param.Corr * existingPeak.Corr; // Envelope correlation-based scoring
                            score += param.MassError * massErrorPpm; // Envelope correlation-based scoring

                            if (baseIonType.IsPrefix)
                            {
                                prefixHit = true;
                            }
                            else
                            {
                                suffixHit = true;
                            }
                        }
                    }
                }

                if (prefixHit && suffixHit)
                {
                    score += ScoreParam.ComplementaryIonCount;
                }
            }
            else
            {
                var ionsFound = new Dictionary<bool, double>();

                foreach (var baseIonType in BaseIonTypes)
                {
                    var fragmentComposition = baseIonType.IsPrefix
                        ? prefixFragmentComposition + baseIonType.OffsetComposition
                        : suffixFragmentComposition + baseIonType.OffsetComposition;

                    var param = baseIonType.IsPrefix ? ScoreParam.Prefix : ScoreParam.Suffix;

                    var massBinNum = _comparer.GetBinNumber(fragmentComposition.Mass);
                    if (massBinNum >= 0 && massBinNum < _comparer.NumberOfBins)
                    {
                        if (_massBinToPeakMap.TryGetValue(massBinNum, out var existingPeak))
                        {
                            var massErrorPpm = 1e6 * (Math.Abs(existingPeak.Mass - fragmentComposition.Mass) / fragmentComposition.Mass);

                            double ionscore = 0;
                            ionscore += param.Count;
                            ionscore += param.Intensity * Math.Min(existingPeak.Intensity / ReferencePeakIntensity, 1.0); // intensity-based scoring
                            ionscore += param.Dist * existingPeak.Dist; // Envelope distance-based scoring
                            ionscore += param.Corr * existingPeak.Corr; // Envelope correlation-based scoring
                            ionscore += param.MassError * massErrorPpm; // Envelope correlation-based scoring

                            if (!ionsFound.ContainsKey(baseIonType.IsPrefix))
                            {
                                ionsFound.Add(baseIonType.IsPrefix, ionscore);
                            }

                            if (ionsFound.ContainsKey(baseIonType.IsPrefix) && ionsFound[baseIonType.IsPrefix] > ionscore)
                            {
                                continue;
                            }

                            ionsFound[baseIonType.IsPrefix] = ionscore;

                            //score += ionscore;

                            if (baseIonType.IsPrefix)
                            {
                                prefixHit = true;
                            }
                            else
                            {
                                suffixHit = true;
                            }
                        }
                    }
                }

                if (prefixHit && suffixHit)
                {
                    score += ScoreParam.ComplementaryIonCount;
                }

                foreach (var ionScore in ionsFound.Values)
                {
                    score += ionScore;
                }
            }

            return score;
        }

        public double?[][] GetNodeScores(double proteinMass)
        {
            var numNodes = _comparer.GetBinNumber(proteinMass) + 1;
            var nodeScores = new double?[2][];
            var prefixFragScores = nodeScores[0] = new double?[numNodes];
            var suffixFragScores = nodeScores[1] = new double?[numNodes];
            var deconvPeaks = (DeconvolutedPeak[])Ms2Spectrum.Peaks;

            if (!InformedProteomics.Backend.Utils.FlipSwitch.UseFlipScoring)
            {
                var prefixOffsetMass = BaseIonTypes[0].OffsetComposition.Mass;
                var suffixOffsetMass = BaseIonTypes[1].OffsetComposition.Mass;

                // assume that peaks are prefixFragment ions
                foreach (var peak in deconvPeaks)
                {
                    var prefixIonMass = peak.Mass;
                    var prefixFragmentMass = prefixIonMass - prefixOffsetMass;
                    var binIndex = _comparer.GetBinNumber(prefixFragmentMass);

                    if (binIndex >= 0 && binIndex < numNodes)
                    {
                        prefixFragScores[binIndex] = GetMatchedIonPeakScore(true, peak);
                    }

                    var suffixIonMass = peak.Mass;
                    var suffixFragmentMass = suffixIonMass - suffixOffsetMass;
                    prefixFragmentMass = proteinMass - suffixFragmentMass;
                    binIndex = _comparer.GetBinNumber(prefixFragmentMass);

                    if (binIndex >= 0 && binIndex < numNodes)
                    {
                        suffixFragScores[binIndex] = GetMatchedIonPeakScore(false, peak);

                        if (prefixFragScores[binIndex] != null) // there are complementary pair ions
                        {
                            suffixFragScores[binIndex] += ScoreParam.ComplementaryIonCount;
                        }
                    }
                }
            }
            else
            {
                foreach (var peak in deconvPeaks)
                {
                    foreach (var ionType in this.BaseIonTypes)
                    {
                        var fragmentMass = peak.Mass - ionType.OffsetComposition.Mass;
                        fragmentMass = ionType.IsPrefix ? peak.Mass : proteinMass - fragmentMass;
                        var binIndex = this._comparer.GetBinNumber(fragmentMass);

                        var nodeSet = ionType.IsPrefix ? prefixFragScores : suffixFragScores;
                        var compNodeSet = !ionType.IsPrefix ? prefixFragScores : suffixFragScores;
                        if (binIndex < 0 || binIndex >= numNodes)
                        {
                            continue;
                        }

                        nodeSet[binIndex] = GetMatchedIonPeakScore(ionType.IsPrefix, peak);
                        if (compNodeSet[binIndex] != null)
                        {
                            nodeSet[binIndex] += ScoreParam.ComplementaryIonCount;
                        }
                    }
                }
            }

            return nodeScores;
        }

        private double GetMatchedIonPeakScore(bool isPrefixIon, DeconvolutedPeak peak)
        {
            var param = (isPrefixIon) ? ScoreParam.Prefix : ScoreParam.Suffix;
            var score = param.Count;
            score += param.Intensity * Math.Min(peak.Intensity / ReferencePeakIntensity, 1.0d);
            score += param.Corr * peak.Corr;
            score += param.Dist * peak.Dist;
            return score;
        }

        private readonly Dictionary<int, DeconvolutedPeak> _massBinToPeakMap;
        private readonly IMassBinning _comparer;
    }
}
