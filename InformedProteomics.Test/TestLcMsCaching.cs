﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.Database;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.Utils;
using InformedProteomics.TopDown.Scoring;
using NUnit.Framework;

namespace InformedProteomics.Test
{
    [TestFixture]
    internal class TestLcMsCaching
    {
        [Test]
        public void TestFloatingPointRounding()
        {
            const double value = 7655.9568537625;
            var converted = BitConverter.DoubleToInt64Bits(value);
            var rounded = (converted >> 37) << 37;
            Console.WriteLine("{0,25:E16}{1,23:X16}{2,23:X16}", value, converted, (converted >> 37) << 37);
            Console.WriteLine("{0}\t{1}", value, BitConverter.Int64BitsToDouble(rounded));
        }

        [Test]
        public void TestDeconvolutionMs2()
        {
            const string rawFilePath = @"C:\cygwin\home\kims336\Data\TopDown\raw\DataFiles\SBEP_STM_001_02272012_Aragon.raw";
            var run = LcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);
            var numSpecs = 0;
            var numPeaks = 0;
            //foreach (var ms2ScanNum in run.GetScanNumbers(2))
            var ms2ScanNum = 1575;
            {
                var spec = run.GetSpectrum(ms2ScanNum) as ProductSpectrum;
                //if (spec == null) continue;
                var deconvolutedSpec = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(spec, 2, 10, new Tolerance(10), 0.7) as ProductSpectrum;
                if (deconvolutedSpec != null)
                {
                    deconvolutedSpec.Display();
                    var nPeaks = deconvolutedSpec.Peaks.Length;
                    //Console.WriteLine("{0}\t{1}", ms2ScanNum, nPeaks);
                    ++numSpecs;
                    numPeaks += nPeaks;
                    var hist = new Dictionary<double, int>();
                    for(var i=0; i<nPeaks-1; i++)
                    {
                        for (var j = i + 1; j < nPeaks; j++)
                        {
                            var sum = deconvolutedSpec.Peaks[i].Mz + deconvolutedSpec.Peaks[j].Mz;
                            var rounded = BitConverter.Int64BitsToDouble((BitConverter.DoubleToInt64Bits(sum) >> 37) << 37);
                            int num;
                            if (hist.TryGetValue(rounded, out num)) hist[rounded] = num + 1;
                            else hist[rounded] = 1;
                        }
                    }
                    Console.WriteLine("{0}\t{1}", ms2ScanNum, nPeaks);
                    foreach (var entry in hist.OrderByDescending(e => e.Value))
                    {
                        var mass = entry.Key;
                        var charge = (int)Math.Round(mass/spec.IsolationWindow.IsolationWindowTargetMz);
                        if(spec.IsolationWindow.Contains(Ion.GetIsotopeMz(mass, charge, Averagine.GetIsotopomerEnvelope(mass).MostAbundantIsotopeIndex)))
                        {
                            Console.WriteLine("{0}\t{1}\t{2}", entry.Key, entry.Value, charge);
                        }
                    }
                }
            }
            Console.WriteLine("NumPeaks: {0:f2} {1}/{2}", numPeaks/(double)numSpecs, numPeaks, numSpecs);
        }

        [Test]
        public void FilteringEfficiency()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            const string rawFilePath = @"C:\cygwin\home\kims336\Data\TopDown\raw\DataFiles\SBEP_STM_001_02272012_Aragon.raw";
            var run = LcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);
            sw.Stop();
            var sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Reading run: {0:f4} sec", sec);

            const int minPrecursorCharge = 3;
            const int maxPrecursorCharge = 30;
            const int tolerancePpm = 10;
            var tolerance = new Tolerance(tolerancePpm);
            sw.Reset();
            sw.Start();
            //var ms1BasedFilter = new Ms1BasedFilter(run, minPrecursorCharge, maxPrecursorCharge, tolerancePpm);
//            
            //var ms1BasedFilter = new Ms1IsotopeTopKFilter(run, minPrecursorCharge, maxPrecursorCharge, tolerancePpm, 20);
            //var ms1BasedFilter = new ProductScorerBasedOnDeconvolutedSpectra(run,
            //    minPrecursorCharge, maxPrecursorCharge,
            //    0, 0,
            //    600.0, 1800.0, new Tolerance(tolerancePpm), null);
            //ms1BasedFilter.CachePrecursorMatchesBinCentric();
            var ms1BasedFilter = new Ms1IsotopeAndChargeCorrFilter(run, minPrecursorCharge, maxPrecursorCharge, 10, 3000, 50000, 0.5, 0.5, 40);
            //var ms1BasedFilter = new Ms1IsotopeCorrFilter(run, minPrecursorCharge, maxPrecursorCharge, 15, 0.5, 40);

            sw.Stop();
            sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Ms1 filter: {0:f4} sec", sec);

            ISequenceFilter ms1Filter = ms1BasedFilter;

            sw.Reset();
            sw.Start();
            const double minProteinMass = 3000.0;
            const double maxProteinMass = 30000.0;
            var minBinNum = ProductScorerBasedOnDeconvolutedSpectra.GetBinNumber(minProteinMass);
            var maxBinNum = ProductScorerBasedOnDeconvolutedSpectra.GetBinNumber(maxProteinMass);
            var numComparisons = 0L;
            for (var binNum = minBinNum; binNum <= maxBinNum; binNum++)
            {
                var mass = ProductScorerBasedOnDeconvolutedSpectra.GetMz(binNum);
                numComparisons += ms1Filter.GetMatchingMs2ScanNums(mass).Count();
            }
            sw.Stop();
            sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Calculating #matches per bin: {0:f4} sec", sec);

            const string resultFilePath = @"C:\cygwin\home\kims336\Data\TopDown\raw\SBEP_STM_001_02272012_Aragon_4PTMs.icresult";
            var tsvReader = new TsvFileParser(resultFilePath);
            var compositions = tsvReader.GetData("Composition");
            var scanNums = tsvReader.GetData("ScanNum");
            var charges = tsvReader.GetData("Charge");
            var scores = tsvReader.GetData("Score");
            var qvalues = tsvReader.GetData("QValue");
            var sequences = tsvReader.GetData("Sequence");

            var sequenceCount = new Dictionary<string, int>();
            for (var i = 0; i < compositions.Count; i++)
            {
                if (qvalues != null)
                {
                    var qValue = Convert.ToDouble(qvalues[i]);
                    if (qValue > 0.01) continue;
                }
                else
                {
                    var score = Convert.ToDouble(scores[i]);
                    if (score < 13) continue;
                }
                var scanNum = Convert.ToInt32(scanNums[i]);
                var charge = Convert.ToInt32(charges[i]);
                var composition = Composition.Parse(compositions[i]);
                var precursorIon = new Ion(composition, charge);
                var spec = run.GetSpectrum(scanNum) as ProductSpectrum;
                var isValid = spec != null && spec.IsolationWindow.Contains(precursorIon.GetMostAbundantIsotopeMz());
                if (!isValid) continue;

                var sequence = sequences[i];
                int count;
                if (sequenceCount.TryGetValue(sequence, out count)) sequenceCount[sequence] = count + 1;
                else sequenceCount[sequence] = 1;
            }
            //var sequences = tsvReader.GetData("Annotation");

            var seqSet = new HashSet<string>();
            var allSeqSet = new HashSet<string>();
            var numUnfilteredSpecs = 0;
            var totalSpecs = 0;
            for (var i = 0; i < compositions.Count; i++)
            {
                if (qvalues != null)
                {
                    var qValue = Convert.ToDouble(qvalues[i]);
                    if (qValue > 0.01) continue;
                }
                else
                {
                    var score = Convert.ToDouble(scores[i]);
                    if (score < 13) continue;
                }
                var scanNum = Convert.ToInt32(scanNums[i]);
                var charge = Convert.ToInt32(charges[i]);
                var composition = Composition.Parse(compositions[i]);
                var precursorIon = new Ion(composition, charge);
                var spec = run.GetSpectrum(scanNum) as ProductSpectrum;
                var isValid = spec != null && spec.IsolationWindow.Contains(precursorIon.GetMostAbundantIsotopeMz());
                if (!isValid) continue;

                ++totalSpecs;

                var precursorScanNum = run.GetPrecursorScanNum(scanNum);
                var precursorSpec = run.GetSpectrum(precursorScanNum);
                var corr1 = precursorSpec.GetCorrScore(precursorIon, tolerance, 0.1);

                var nextScanNum = run.GetNextScanNum(scanNum, 1);
                var nextSpec = run.GetSpectrum(nextScanNum);
                var corr2 = nextSpec.GetCorrScore(precursorIon, tolerance, 0.1);

                var corr3 = ms1Filter.GetMatchingMs2ScanNums(composition.Mass).Contains(scanNum) ? 1 : 0;
                if (corr3 == 1)
                {
                    numUnfilteredSpecs++;
                    seqSet.Add(sequences[i]);
                }
                allSeqSet.Add(sequences[i]);

                //var xic = run.GetFullExtractedIonChromatogram(precursorIon.GetMostAbundantIsotopeMz(), tolerance);
                ////xic.Display();
                //var apexScanNum = xic.GetNearestApexScanNum(run.GetPrecursorScanNum(scanNum), false);
                //var apexSpec = run.GetSpectrum(apexScanNum);
                //var corr3 = apexSpec.GetCorrScore(precursorIon, tolerance, 0.1);

                var corrMax = new[] { corr1, corr2, corr3 }.Max();

                Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", scanNum, precursorScanNum, corr1, nextScanNum, corr2, corr3, corrMax, sequenceCount[sequences[i]]);
            }

            Console.WriteLine("TotalNumComparisons: {0}", numComparisons);
            Console.WriteLine("AverageNumComparisons: {0:f2}", numComparisons/(double)(maxBinNum-minBinNum+1));
            Console.WriteLine("SuccessRate: {0:f2} {1} / {2}", numUnfilteredSpecs/(double)totalSpecs, numUnfilteredSpecs, totalSpecs);
            Console.WriteLine("NumUniqueSequences: {0:f2}, {1} / {2}", seqSet.Count/(double)allSeqSet.Count, seqSet.Count, allSeqSet.Count);
            sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }


        [Test]
        public void TestPossibleSequenceMasses()
        {
            const string rawFilePath = @"C:\cygwin\home\kims336\Data\TopDown\raw\DataFiles\SBEP_STM_001_02272012_Aragon.raw";
            var run = LcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            //var ms1BasedFilter = new Ms1IsotopeCorrFilter(run, 3, 30, 15, 0.7, 1000);
            var ms1BasedFilter = new Ms1IsotopeAndChargeCorrFilter(run);
//          var masses = ms1BasedFilter.GetPossibleSequenceMasses(1113);

            //var ms1BasedFilter = new Ms1IsotopeTopKFilter(run, 3, 30, 15);
            //var masses = ms1BasedFilter.GetPossibleSequenceMasses(2819, 20);
            //foreach (var m in masses)
            //{
            //    Console.WriteLine(m);
            //}
            sw.Stop();
            var sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }

        [Test]
        public void TestMs1Filtering()
        {
            const string resultFilePath =
            //    @"C:\cygwin\home\kims336\Data\TopDown\raw\CorrMatches_N30\SBEP_STM_001_02272012_Aragon.tsv";
                @"C:\cygwin\home\kims336\Data\TopDown\raw\CorrMatches_N30\SBEP_STM_001_02272012_Aragon.decoy.icresult";
                
            const string rawFilePath = @"C:\cygwin\home\kims336\Data\TopDown\raw\DataFiles\SBEP_STM_001_02272012_Aragon.raw";
            var run = LcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);

            //const int minPrecursorCharge = 3;
            //const int maxPrecursorCharge = 30;
            const int tolerancePpm = 15;
            var tolerance = new Tolerance(15);

            //var ms1BasedFilter = new Ms1IsotopeCorrFilter(run, minPrecursorCharge, maxPrecursorCharge, tolerancePpm, 0.7, 40);
            ////var ms1BasedFilter = new Ms1IsotopeTopKFilter(run, minPrecursorCharge, maxPrecursorCharge, tolerancePpm, 20);
            //ISequenceFilter ms1Filter = ms1BasedFilter;

            var tsvReader = new TsvFileParser(resultFilePath);
            var compositions = tsvReader.GetData("Composition");
            var scanNums = tsvReader.GetData("ScanNum");
            var charges = tsvReader.GetData("Charge");
            var qValues = tsvReader.GetData("QValue");
            var scores = tsvReader.GetData("Score");

            //var sequences = tsvReader.GetData("Annotation");

            //var hist = new int[11];

            Console.WriteLine("ScanNum\tScore\tPrecursor\tNext\tSum\tNextIsotope\tLessCharge\tMoreCharge\tMax\tNumXicPeaks");
            for (var i = 0; i < compositions.Count; i++)
            {

                if (qValues != null)
                {
                    var qValue = Convert.ToDouble(qValues[i]);
                    if (qValue > 0.01) continue;
                }

                var scanNum = Convert.ToInt32(scanNums[i]);
                var composition = Composition.Parse(compositions[i]);
                var charge = Convert.ToInt32(charges[i]);

                var precursorIon = new Ion(composition, charge);
                var spec = run.GetSpectrum(scanNum) as ProductSpectrum;
                var isValid = spec != null && spec.IsolationWindow.Contains(precursorIon.GetMostAbundantIsotopeMz());
                if (!isValid) continue;

                var score = Convert.ToDouble(scores[i]);

                var precursorScanNum = run.GetPrecursorScanNum(scanNum);
                var precursorSpec = run.GetSpectrum(precursorScanNum);
                var preIsotopeCorr = precursorSpec.GetCorrScore(precursorIon, tolerance, 0.1);

                var nextScanNum = run.GetNextScanNum(scanNum, 1);
                var nextSpec = run.GetSpectrum(nextScanNum);
                var nextIsotopeCorr = nextSpec.GetCorrScore(precursorIon, tolerance, 0.1);

                var xicMostAbundant = run.GetExtractedIonChromatogram(precursorIon.GetMostAbundantIsotopeMz(), tolerance, scanNum);

                var apexScanNum = xicMostAbundant.GetApexScanNum();
                if (apexScanNum < run.MinLcScan) apexScanNum = scanNum;
                var sumSpec = run.GetSummedMs1Spectrum(apexScanNum);
                var apexIsotopeCorr = sumSpec.GetCorrScore(precursorIon, tolerance, 0.1);
//                var corr3 = ms1Filter.GetMatchingMs2ScanNums(composition.Mass).Contains(scanNum) ? 1 : 0;

                var xicNextIsotope = run.GetExtractedIonChromatogram(precursorIon.GetMostAbundantIsotopeMz() + Constants.C13MinusC12/charge, tolerance, scanNum);

                var plusOneIsotopeCorr = xicMostAbundant.GetCorrelation(xicNextIsotope);

                var precursorIonChargeMinusOne = new Ion(composition, charge - 1);
                var xicChargeMinusOne = run.GetExtractedIonChromatogram(precursorIonChargeMinusOne.GetMostAbundantIsotopeMz(), tolerance, scanNum);
                var chargeMinusOneCorr = xicMostAbundant.GetCorrelation(xicChargeMinusOne);

                var precursorIonChargePlusOne = new Ion(composition, charge + 1);
                var xicChargePlusOne = run.GetExtractedIonChromatogram(precursorIonChargePlusOne.GetMostAbundantIsotopeMz(), tolerance, scanNum);
                var chargePlusOneCorr = xicMostAbundant.GetCorrelation(xicChargePlusOne);

                var max = new[] {preIsotopeCorr, nextIsotopeCorr, apexIsotopeCorr, plusOneIsotopeCorr, chargeMinusOneCorr, chargePlusOneCorr}.Max();
                Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}", 
                    scanNum, score, preIsotopeCorr, nextIsotopeCorr, apexIsotopeCorr, plusOneIsotopeCorr, chargeMinusOneCorr, chargePlusOneCorr, max, xicMostAbundant.Count);
            }

            //Console.WriteLine("Histogram");
            //for (var i = 0; i < hist.Length; i++)
            //{
            //    Console.WriteLine("{0:f1}\t{1}", i / 10.0, hist[i]);
            //}
        }

        [Test]
        public void TestMs1Caching()
        {
            const string rawFilePath = @"C:\cygwin\home\kims336\Data\TopDown\raw\DataFiles\SBEP_STM_001_02272012_Aragon.raw";
            var run = LcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var ms1BasedFilter = new Ms1IsotopeCorrFilter(run);
            var testComposition = Composition.Parse("C(331) H(508) N(88) O(100) S(0)");
            Console.WriteLine("Mass: {0}", testComposition.Mass);
            foreach (var ms2ScanNum in ms1BasedFilter.GetMatchingMs2ScanNums(testComposition.Mass))
            {
                Console.WriteLine(ms2ScanNum);
            }
            sw.Stop();
            var sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }

        [Test]
        public void TestDeconvolution()
        {
            const int minProductIonCharge = 1; 
            const int maxProductIonCharge = 10;
            var productIonTolerance = new Tolerance(10);

            const string rawFilePath = @"C:\cygwin\home\kims336\Data\TopDown\raw\DataFiles\SBEP_STM_001_02272012_Aragon.raw";
            //var run = LcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);
            var run = LcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun, 0, 0);

            var spec = run.GetSpectrum(2313);

            var deconvolutedSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(spec, minProductIonCharge, maxProductIonCharge, productIonTolerance, 0.2);
            deconvolutedSpectrum.Display();
        }

        [Test]
        public void TestMs2Caching()
        {
            const string rawFilePath = @"C:\cygwin\home\kims336\Data\TopDown\raw\DataFiles\SBEP_STM_001_02272012_Aragon.raw";
            var run = LcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);

            const int minPrecursorIonCharge = 3; // 3
            const int maxPrecursorIonCharge = 30;// 67
            const int minProductIonCharge = 1;
            const int maxProductIonCharge = 10;

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var runCache = new ProductScorerBasedOnDeconvolutedSpectra(run);
            runCache.DeconvoluteProductSpectra();
            sw.Stop();
            var sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }


        [Test]
        public void TestAveragine()
        {
            //for (var nominalMass = 1000; nominalMass <= 1000; nominalMass++)
            //{
            //    Console.WriteLine("{0}\t{1}", nominalMass,
            //        string.Join(",", Averagine.GetIsotopomerEnvelopeFromNominalMass(nominalMass).Envolope.Select(v => string.Format("{0:f3}", v))));
            //}
            for (var nominalMass = 1000; nominalMass <= 50000; nominalMass++)
            {
                var averagine = Averagine.GetIsotopomerEnvelopeFromNominalMass(nominalMass);
            }
        }

        [Test]
        public void TestMs1Signature()
        {
            const string rawFilePath = @"C:\cygwin\home\kims336\Data\TopDown\raw\DataFiles";

            const string resultPath = @"C:\cygwin\home\kims336\Data\TopDown\raw\CorrMatches_N30";
            foreach (var resultFilePath in Directory.GetFiles(resultPath, "*.tsv"))
            {
                Console.WriteLine(resultFilePath);
            }

        }

        public void TestNominalMassErrors()
        {
            const int minLength = 300;
            const int maxLength = 400;

            var sw = new System.Diagnostics.Stopwatch();

//            const string dbFile = @"\\protoapps\UserData\Sangtae\TestData\H_sapiens_Uniprot_SPROT_2013-05-01_withContam.fasta";
            const string dbFile = @"C:\cygwin\home\kims336\Data\TopDownJia\database\ID_003962_71E1A1D4.fasta";
            //const string dbFile = @"C:\cygwin\home\kims336\Data\TopDownJia\database\TargetProteins.fasta";
            var db = new FastaDatabase(dbFile);
            db.Read();
            var indexedDb = new IndexedDatabase(db);
            var numSequences = 0L;
            sw.Start();

            var hist = new long[11];
            var aaSet = new AminoAcidSet();
            foreach (var peptideAnnotationAndOffset in indexedDb.AnnotationsAndOffsetsNoEnzyme(minLength, maxLength))
            {
                ++numSequences;
                var annotation = peptideAnnotationAndOffset.Annotation;
                var sequenceStr = annotation.Substring(2, annotation.Length - 4);
                var sequenceComp = aaSet.GetComposition(sequenceStr);
                var mass = sequenceComp.Mass;
                var nominalMass = sequenceComp.NominalMass;
                var error = (int) Math.Round(mass*Constants.RescalingConstant) - nominalMass;
                var errorBin = error + hist.Length/2;
                if (errorBin < 0) errorBin = 0;
                if (errorBin >= hist.Length) errorBin = hist.Length - 1;
                hist[errorBin]++;
            }

            Console.WriteLine("NumSequences: {0}", numSequences);
            for (var i = 0; i < hist.Length; i++)
            {
                Console.WriteLine("{0}\t{1}\t{2}", i - hist.Length/2, hist[i], hist[i]/(double)numSequences);
            }

            sw.Stop();
            var sec = sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine(@"Elapsed Time: {0:f4} sec", sec);
        }
    }
}