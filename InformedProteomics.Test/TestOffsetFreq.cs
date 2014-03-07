﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.Utils;
using NUnit.Framework;

namespace InformedProteomics.Test
{
    public class IonProbability
    {
        public int Found { get; set; }
        public int Total { get; set; }
        public IonProbability(int f, int t) { Found = f; Total = t; }
    }

    [TestFixture]
    public class OffsetTableTemp
    {
        private string _fileList;
        private string _preRes;
        private string _preRaw;
        private string _outPre;
        private string _outFileName;
        private string _outSuff;
        private double _pepQThreshold;
        private int _precursorCharge;
        private string[] _ionTypes;
        private IonTypeFactory _ionTypeFactory;
        private ActivationMethod _act;
        
        private const string PrecChargeHeader = "Charge";
        private const string ScanHeader = "ScanNum";
        private const string PeptideHeader = "Peptide";
        private const string PepQValueHeader = "PepQValue";
        private const string FormulaHeader = "Formula";
        const double RelativeIntensityThreshold = 1.0;
        readonly Tolerance _defaultTolerance = new Tolerance(15, ToleranceUnit.Ppm);

        private IEnumerable<Tuple<string, Spectrum>> CleanScans(string txtFileName, string rawFileName)
        {
            var tsvParser = new TsvFileParser(txtFileName);

            var scans = tsvParser.GetData(ScanHeader);
            var peptides = tsvParser.GetData(PeptideHeader);
            var charges = tsvParser.GetData(PrecChargeHeader);
            var pepQValues = tsvParser.GetData(PepQValueHeader);
            var compositions = tsvParser.GetData(FormulaHeader);

//            var lcms = LcMsRun.GetLcMsRun(rawFileName, MassSpecDataType.XCaliburRun, 1.4826, 1.4826);
            var lcms = LcMsRun.GetLcMsRun(rawFileName, MassSpecDataType.XCaliburRun, 0, 0);

            var clean = new List<Tuple<string, Spectrum>>();
            var numRows = scans.Count;
            var peptideSet = new HashSet<string>();

            for (var i = 0; i < numRows; i++)
            {
                if (Convert.ToDouble(pepQValues[i]) > _pepQThreshold) continue;

                var precCharge = Convert.ToInt32(charges[i]);
                if (precCharge != _precursorCharge) continue;

                var peptide = peptides[i];
                if (!peptideSet.Add(peptide)) continue;

                var sequence = Sequence.GetSequenceFromMsGfPlusPeptideStr(peptide);
                var composition = Composition.Parse(compositions[i]);
                Assert.True(composition.Equals(sequence.Composition+Composition.H2O));
                var spec = lcms.GetSpectrum(Convert.ToInt32(scans[i]));
                clean.Add(new Tuple<string, Spectrum>(peptide, spec));
            }

            return clean;
        }

        private Dictionary<string, IonProbability> GetOffsetCounts(IEnumerable<Tuple<string, Spectrum>> cleanScans,
                                        string[] ionTypes, ActivationMethod act, IonTypeFactory ionTypeFactory)
        {
            var probabilities = new Dictionary<string, IonProbability>();
            foreach (string ionTypeStr in ionTypes)
            {
                if (!probabilities.ContainsKey(ionTypeStr))
                    probabilities.Add(ionTypeStr, new IonProbability(0, 0));
            }

            var pepDict = new Dictionary<string, int>();

            foreach (var node in cleanScans)
            {
                var protein = node.Item1;
                var spectrum = node.Item2;

                var sequence = Sequence.GetSequenceFromMsGfPlusPeptideStr(protein);
                var spec = spectrum as ProductSpectrum;
                if (spec == null) continue;
                for (int i = 1; i < protein.Length; i++)
                {
                    if (spec.ActivationMethod == act)
                    {
                        foreach (var ionTypeStr in ionTypes)
                        {
                            Composition sequenceComposition = null;
                            if (ionTypeStr[0] == 'a' || ionTypeStr[0] == 'b' || ionTypeStr[0] == 'c')
                                sequenceComposition = sequence.GetComposition(0, i);
                            else if (ionTypeStr[0] == 'x' || ionTypeStr[0] == 'y' || ionTypeStr[0] == 'z')
                                sequenceComposition = sequence.GetComposition(protein.Length - i, protein.Length);
                            else
                                throw new FormatException();

                            var ionType = ionTypeFactory.GetIonType(ionTypeStr);
                            var ion = ionType.GetIon(sequenceComposition);
                            ion.Composition.ComputeApproximateIsotopomerEnvelop();

//                            var mz = ion.GetMonoIsotopicMz();
//                            var peak = spec.FindPeak(mz, defaultTolerance);

                            probabilities[ionTypeStr].Total++;
                            if (spec.ContainsIon(ion, _defaultTolerance, RelativeIntensityThreshold))
//                            if (peak != null)
                                probabilities[ionTypeStr].Found++;

                            // Added by Sangtae for debugging
                            //Console.WriteLine("{0}{1} {2} {3}", ionTypeStr, i, ion.GetMonoIsotopicMz(), spec.ContainsIon(ion, _defaultTolerance, RelativeIntensityThreshold));
                        }
                    }
                }
            }
            return probabilities;
        }

        private void WriteProbFile(string outFile, Dictionary<string, IonProbability> offsetCounts, string[] ionTypes)
        {
            using (var file = new StreamWriter(outFile))
            {
                for (int i = 0; i < ionTypes.Length; i++)
                {
                    file.WriteLine("{0}\t{1}", ionTypes[i], Math.Round((double)(offsetCounts[ionTypes[i]].Found) / (offsetCounts[ionTypes[i]].Total), 5));
                }
            }
        }

        private void WriteOffsetCountsFile(string outFile, Dictionary<string, IonProbability> offsetCounts, string[] ionTypes)
        {
            using (var file = new StreamWriter(outFile))
            {
                for (int i = 0; i < ionTypes.Length; i++)
                {
                    file.Write("{0}Found\t{0}Total", ionTypes[i]);
                    if (i != ionTypes.Length - 1)
                        file.Write("\t");
                }
                file.WriteLine();
                for (int i = 0; i < ionTypes.Length; i++)
                {
                    file.Write("{0}\t{1}", offsetCounts[ionTypes[i]].Found, offsetCounts[ionTypes[i]].Total);
                    if (i != ionTypes.Length - 1)
                        file.Write("\t");
                }
            }
        }

        private void InitTest(INIReader reader)
        {
            // Read program variables
            var config = reader.getNodes("vars").First();
            _precursorCharge = Convert.ToInt32(config.Contents["precursorcharge"]);
            _pepQThreshold = Convert.ToDouble(config.Contents["pepqvalue"]);
            var actStr = config.Contents["activationmethod"].ToLower();
            switch (actStr)
            {
                case "hcd":
                    _act = ActivationMethod.HCD;
                    break;
                case "cid":
                    _act = ActivationMethod.CID;
                    break;
                case "etd":
                    _act = ActivationMethod.ETD;
                    break;
            }

            // Read ion data
            var ionInfo = reader.getNodes("ion").First();
            int totalCharges = Convert.ToInt32(ionInfo.Contents["totalcharges"]);
            var ionNames = ionInfo.Contents["ions"].Split(',');
            _ionTypes = new string[ionNames.Length];
            Array.Copy(ionNames, _ionTypes, _ionTypes.Length);
            var ionTypeStr = ionInfo.Contents["iontype"].Split(',');
            var ions = new BaseIonType[ionTypeStr.Length];
            for (int i = 0; i < ionTypeStr.Length; i++)
            {
                switch (ionTypeStr[i].ToLower())
                {
                    case "a":
                        ions[i] = BaseIonType.A;
                        break;
                    case "b":
                        ions[i] = BaseIonType.B;
                        break;
                    case "c":
                        ions[i] = BaseIonType.C;
                        break;
                    case "x":
                        ions[i] = BaseIonType.X;
                        break;
                    case "y":
                        ions[i] = BaseIonType.Y;
                        break;
                    case "z":
                        ions[i] = BaseIonType.Z;
                        break;
                }
            }
            var ionLossStr = ionInfo.Contents["losses"].Split(',');
            var ionLosses = new NeutralLoss[ionLossStr.Length];
            for (int i = 0; i < ionLossStr.Length; i++)
            {
                switch (ionLossStr[i].ToLower())
                {
                    case "noloss":
                        ionLosses[i] = NeutralLoss.NoLoss;
                        break;
                    case "nh3":
                        ionLosses[i] = NeutralLoss.NH3;
                        break;
                    case "h2o":
                        ionLosses[i] = NeutralLoss.H2O;
                        break;
                }
            }
            _ionTypeFactory = new IonTypeFactory(ions, ionLosses, totalCharges);

            // Read input and output file names
            var fileInfo = reader.getNodes("fileinfo").First();
            var name = fileInfo.Contents["name"];
            var fileListtemp = fileInfo.Contents["filelist"];
            _fileList = fileListtemp.Replace("@", name);

            var tsvtemp = fileInfo.Contents["tsvpath"];
            _preRes = tsvtemp.Replace("@", name);

            var rawtemp = fileInfo.Contents["rawpath"];
            _preRaw = rawtemp.Replace("@", name);

            var outPathtemp = fileInfo.Contents["outpath"];
            outPathtemp = outPathtemp.Replace("@", name);
            _outPre = outPathtemp.Replace("*", _precursorCharge.ToString());

            var outFiletemp = fileInfo.Contents["outfile"];
            outFiletemp = outFiletemp.Replace("@", name);
            _outFileName = _outPre + outFiletemp.Replace("*", _precursorCharge.ToString());

            var outSufftemp = fileInfo.Contents["outsuff"];
            outSufftemp = outSufftemp.Replace("@", name);
            _outSuff = outSufftemp.Replace("*", _precursorCharge.ToString());
        }

        [Test]
        public void OffsetFreq()
        {
            InitTest(new INIReader(@"C:\Users\wilk011\Documents\DataFiles\OffsetFreqConfig.ini"));

            var fileNameParser = new TsvFileParser(_fileList);

            var txtFiles = fileNameParser.GetData("text");
            var rawFiles = fileNameParser.GetData("raw");

            var found = new int[_ionTypes.Length];
            var total = new int[_ionTypes.Length];

            for (int i = 0; i < _ionTypes.Length; i++)
            {
                found[i] = 0;
                total[i] = 0;
            }

            using (var txtFileIt = txtFiles.GetEnumerator())
            using (var rawFileIt = rawFiles.GetEnumerator())
            {
                while (txtFileIt.MoveNext() && rawFileIt.MoveNext())
                {
                    string textFile = _preRes + txtFileIt.Current;
                    string rawFile = _preRaw + rawFileIt.Current;
                    Console.WriteLine(rawFile);
                    var scans = CleanScans(textFile, rawFile);
                    var cleanScans = scans as Tuple<string, Spectrum>[] ?? scans.ToArray();

                    var offsetCounts = GetOffsetCounts(cleanScans, _ionTypes, _act, _ionTypeFactory);

                    for (int i = 0; i < _ionTypes.Length; i++)
                    {
                        found[i] += Convert.ToInt32(offsetCounts[_ionTypes[i]].Found);
                        total[i] += Convert.ToInt32(offsetCounts[_ionTypes[i]].Total);
                    }
                }
            }

            using (var finalOutputFile = new StreamWriter(_outFileName))
            {
                for (int i = 0; i < _ionTypes.Length; i++)
                {
                    finalOutputFile.WriteLine("{0}\t{1}", _ionTypes[i], Math.Round((double)(found[i]) / (total[i]), 5));
                }
            }
        }
    }

    class Node
    {
        public string Header { get; private set; }      // The header tag

        // All of the key/value pairs:
        public Dictionary<String, String> Contents { get; private set; }

        // constructor
        public Node(string header, Dictionary<String, String> contents)
        {
            Header = header;
            Contents = contents;
        }
    }
    class INIReader
    {
        public List<Node> Nodes { get; protected set; }

        // Exception
        public class InvalidHeader : Exception { }

        private bool ValidHeader(string header)
        {
            return (header[0] == '[' && header[header.Length - 1] == ']');
        }

        /* read() Read the file and store the result
         * in Nodes.
         */
        private void read(string fileName)
        {
            Node currentNode = null;
            Dictionary<String, String> keyvalue = new Dictionary<String, String>();
            string[] lines = System.IO.File.ReadAllLines(fileName);
            char[] headerbrackets = { '[', ']' };
            string header = "";
            foreach (var line in lines)
            {
                string commentsStripped = line.Split('#')[0];      // remove comments
                string[] parts = commentsStripped.Split('=');       // split key/value
                if (parts.Length < 2)
                {
                    // The line is either a header, empty line, or invalid
                    parts[0] = parts[0].Trim().ToLower();
                    if (parts[0] == "")
                        // empty line
                        continue;
                    else if (currentNode == null)
                    {
                        // first node in the file
                        currentNode = new Node(null, null);
                        header = parts[0].Trim(headerbrackets);
                    }
                    else if (currentNode != null)
                    {
                        // this isn't the first node in the file
                        currentNode = new Node(header, keyvalue);
                        keyvalue = new Dictionary<String, String>();
                        Nodes.Add(currentNode);
                        header = parts[0].Trim(headerbrackets);
                    }
                    if (!ValidHeader(parts[0]))
                        // invalid header
                        throw new InvalidHeader();
                }
                else
                {
                    // key value pair
                    string key = parts[0].Trim().ToLower();
                    string value = parts[1].Trim();
                    keyvalue.Add(key, value);
                }
            }
            currentNode = new Node(header, keyvalue);
            Nodes.Add(currentNode);
        }

        /*
         *  Constructor
         */
        public INIReader(string fileName)
        {
            Nodes = new List<Node>();
            read(fileName);
        }


        /*
         * getNodes() return a list of all the nodes with a particular
         * header tag.
         */
        public List<Node> getNodes(string headerTag)
        {
            return (from i in Nodes
                    where i.Header == headerTag
                    select i).ToList();
        }
    }
}