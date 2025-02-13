﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.Database;

namespace InformedProteomics.Backend.SearchResults
{
    /// <summary>
    /// Computes the False Decoy Ratio and scores for the supplied target and decoy hits
    /// </summary>
    public class FdrCalculator
    {
        // Ignore Spelling: Qvalue

        private const int UNDEFINED_QVALUE = 10;

        private readonly bool _multiplePeptidesPerScan;
        private List<DatabaseSearchResultData> searchResults = new List<DatabaseSearchResultData>();
        private readonly List<DatabaseSearchResultData> filteredResults = new List<DatabaseSearchResultData>();

        /// <summary>
        /// Number of PSMs with a QValue &lt; 0.01
        /// </summary>
        public int NumPSMs { get; private set; }

        /// <summary>
        /// Number of peptides with a PepQValue &lt; 0.01
        /// </summary>
        public int NumPeptides { get; private set; }

        /// <summary>
        /// Error message, if FDR calculation fails
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// The full list of filtered results, with FDR scores added
        /// </summary>
        public List<DatabaseSearchResultData> FilteredResults => new List<DatabaseSearchResultData>(filteredResults);

        /// <summary>
        /// Instantiate the FDR calculator
        /// </summary>
        /// <param name="targetResultFilePath"></param>
        /// <param name="decoyResultFilePath"></param>
        /// <param name="multiplePeptidesPerScan"></param>
        /// <remarks>If an error occurs, ErrorMessage will be non-null</remarks>
        public FdrCalculator(string targetResultFilePath, string decoyResultFilePath, bool multiplePeptidesPerScan = false)
        {
            ErrorMessage = string.Empty;

            _multiplePeptidesPerScan = multiplePeptidesPerScan;

            // Read the data and add it to the list
            if (!ReadTargetAndDecoy(targetResultFilePath, decoyResultFilePath))
            {
                if (string.IsNullOrWhiteSpace(ErrorMessage))
                {
                    ErrorMessage = "ReadTargetAndDecoy returned false in FdrCalculator";
                }

                return;
            }

            // Add "Qvalue"
            if (!CalculateQValues())
            {
                if (string.IsNullOrWhiteSpace(ErrorMessage))
                {
                    ErrorMessage = "CalculateQValues returned false";
                }

                return;
            }

            // Add "PepQvalue"
            if (!CalculatePepQValues())
            {
                if (string.IsNullOrWhiteSpace(ErrorMessage))
                {
                    ErrorMessage = "CalculatePepQValues returned false";
                }
            }
        }

        /// <summary>
        /// Instantiate the FDR calculator
        /// </summary>
        /// <param name="targetResults"></param>
        /// <param name="decoyResults"></param>
        /// <param name="multiplePeptidesPerScan"></param>
        public FdrCalculator(
            IReadOnlyCollection<DatabaseSearchResultData> targetResults,
            IReadOnlyCollection<DatabaseSearchResultData> decoyResults,
            bool multiplePeptidesPerScan = false)
        {
            ErrorMessage = string.Empty;

            _multiplePeptidesPerScan = multiplePeptidesPerScan;

            // Add the data to the list
            if (!AddTargetAndDecoyData(targetResults, decoyResults))
            {
                if (string.IsNullOrWhiteSpace(ErrorMessage))
                {
                    ErrorMessage = "AddTargetAndDecoy returned false in FdrCalculator";
                }

                return;
            }

            // Add "Qvalue"
            if (!CalculateQValues())
            {
                if (string.IsNullOrWhiteSpace(ErrorMessage))
                {
                    ErrorMessage = "CalculateQValues returned false";
                }

                return;
            }

            // Add "PepQvalue"
            if (!CalculatePepQValues())
            {
                if (string.IsNullOrWhiteSpace(ErrorMessage))
                {
                    ErrorMessage = "CalculatePepQValues returned false";
                }
            }
        }

        /// <summary>
        /// True if there was an error calculating the FDR scores
        /// </summary>
        /// <returns></returns>
        public bool HasError()
        {
            return !string.IsNullOrWhiteSpace(ErrorMessage);
        }

        /// <summary>
        /// Write the results with the FDR data to the specified file
        /// </summary>
        /// <param name="outputFilePath"></param>
        /// <param name="includeDecoy">When true, include decoy-based results in the _IcTda.tsv file</param>
        public void WriteTo(string outputFilePath, bool includeDecoy = false)
        {
            //var resultsToUse = searchResults;
            var resultsToUse = filteredResults;

            IEnumerable<DatabaseSearchResultData> resultsToWrite;
            if (includeDecoy)
            {
                resultsToWrite = resultsToUse;
            }
            else
            {
                resultsToWrite = resultsToUse.Where(x => !x.ProteinName.StartsWith(FastaDatabaseConstants.DecoyProteinPrefix));
            }

            if (!_multiplePeptidesPerScan)
            {
                DatabaseSearchResultData.WriteResultsToFile(outputFilePath, resultsToWrite, true);
                return;
            }

            // Group together results from the same scan
            var scanOrder = new List<int>();
            var matchesByScan = new Dictionary<int, List<DatabaseSearchResultData>>();

            foreach (var match in resultsToWrite)
            {
                if (matchesByScan.TryGetValue(match.ScanNum, out var scanMatches))
                {
                    scanMatches.Add(match);
                    continue;
                }

                matchesByScan.Add(match.ScanNum, new List<DatabaseSearchResultData> { match });
                scanOrder.Add(match.ScanNum);
            }

            var resultsToWriteScanGrouped = new List<DatabaseSearchResultData>();
            foreach (var scanMatches in scanOrder.Select(scanNumber => matchesByScan[scanNumber]))
            {
                resultsToWriteScanGrouped.AddRange(scanMatches);
            }

            DatabaseSearchResultData.WriteResultsToFile(outputFilePath, resultsToWriteScanGrouped, true);
        }

        private bool CalculateQValues()
        {
            // Order by EValue, then Probability, then take only the best scoring result for each scan number
            var distinctSorted = searchResults.OrderBy(r => r.EValue)
                .ThenByDescending(r => r.Probability)
                .GroupBy(r => r.ScanNum)
                .Select(grp => grp.First())
                .ToArray();

            NumPSMs = 0;

            var resultIDsWithQValue = new SortedSet<int>();
            foreach (var item in distinctSorted)
            {
                resultIDsWithQValue.Add(item.ResultID);
            }

            // Calculate q values
            var numDecoy = 0;
            var numTarget = 0;
            var fdr = new double[distinctSorted.Length];
            for (var i = 0; i < distinctSorted.Length; i++)
            {
                var result = distinctSorted[i];
                if (result.ProteinName.StartsWith(FastaDatabaseConstants.DecoyProteinPrefix))
                {
                    numDecoy++;
                }
                else
                {
                    numTarget++;
                }

                fdr[i] = Math.Min(numDecoy / (double)numTarget, 1.0);
            }

            var qValue = new double[fdr.Length];
            qValue[fdr.Length - 1] = fdr[fdr.Length - 1];
            for (var i = fdr.Length - 2; i >= 0; i--)
            {
                qValue[i] = Math.Min(qValue[i + 1], fdr[i]);
                if (qValue[i] <= 0.01)
                {
                    ++NumPSMs;
                }
            }

            for (var i = 0; i < distinctSorted.Length; i++)
            {
                distinctSorted[i].QValue = qValue[i];
            }

            filteredResults.AddRange(distinctSorted);

            if (_multiplePeptidesPerScan)
            {
                // Append the non-rank 1 peptides
                foreach (var item in searchResults)
                {
                    if (resultIDsWithQValue.Contains(item.ResultID))
                    {
                        continue;
                    }

                    // Set the QValue and PepQValue to 10 since those values are not accurate for results with rank 2 or higher
                    item.QValue = UNDEFINED_QVALUE;
                    item.PepQValue = UNDEFINED_QVALUE;
                    filteredResults.Add(item);
                }
            }

            return true;
        }

        private bool CalculatePepQValues()
        {
            IEnumerable<DatabaseSearchResultData> distinctSorting = searchResults
                .OrderBy(r => r.EValue)
                .ThenByDescending(r => r.Probability);

            IEnumerable<DatabaseSearchResultData> distinctSortingOnePerScan;

            if (_multiplePeptidesPerScan)
            {
                distinctSortingOnePerScan = distinctSorting;
            }
            else
            {
                distinctSortingOnePerScan = distinctSorting
                    .Where(r => r.QValue < UNDEFINED_QVALUE)
                    .GroupBy(r => r.ScanNum)
                    .Select(grp => grp.First());
            }

            var distinctSorted = distinctSortingOnePerScan
                .GroupBy(r => r.SequenceWithEnds)
                .Select(grp => grp.First())
                .ToArray();

            // Calculate QValues
            var numDecoy = 0;
            var numTarget = 0;
            var fdr = new double[distinctSorted.Length];
            var peptide = new string[distinctSorted.Length];
            for (var i = 0; i < distinctSorted.Length; i++)
            {
                var row = distinctSorted[i];
                if (row.ProteinName.StartsWith(FastaDatabaseConstants.DecoyProteinPrefix))
                {
                    numDecoy++;
                }
                else
                {
                    numTarget++;
                }

                fdr[i] = Math.Min(numDecoy / (double)numTarget, 1.0);
                peptide[i] = row.SequenceWithEnds;
            }

            var pepQValue = new double[fdr.Length];
            pepQValue[fdr.Length - 1] = fdr[fdr.Length - 1];
            for (var i = fdr.Length - 2; i >= 0; i--)
            {
                pepQValue[i] = Math.Min(pepQValue[i + 1], fdr[i]);
                if (pepQValue[i] <= 0.01)
                {
                    ++NumPeptides;
                }
            }

            var annotationToPepQValue = new Dictionary<string, double>();
            for (var i = 0; i < distinctSorted.Length; i++)
            {
                annotationToPepQValue[peptide[i]] = pepQValue[i];
            }

            foreach (var item in filteredResults.Where(item => item.QValue < UNDEFINED_QVALUE))
            {
                item.PepQValue = annotationToPepQValue[item.SequenceWithEnds];
            }

            return true;
        }

        private bool ReadTargetAndDecoy(string targetResultFilePath, string decoyResultFilePath)
        {
            const string errorBase = "Cannot compute FDR Scores; ";

            if (!File.Exists(targetResultFilePath))
            {
                ErrorMessage = errorBase + "target results file not found, " + Path.GetFileName(targetResultFilePath);
                return false;
            }

            if (!File.Exists(decoyResultFilePath))
            {
                ErrorMessage = errorBase + "decoy results file not found, " + Path.GetFileName(decoyResultFilePath);
                return false;
            }

            var targetData = DatabaseSearchResultData.ReadResultsFromFile(targetResultFilePath);
            var decoyData = DatabaseSearchResultData.ReadResultsFromFile(decoyResultFilePath);

            return AddTargetAndDecoyData(targetData, decoyData);
        }

        private bool AddTargetAndDecoyData(IReadOnlyCollection<DatabaseSearchResultData> targetResults, IReadOnlyCollection<DatabaseSearchResultData> decoyResults)
        {
            const string errorBase = "Cannot compute FDR Scores; ";
            if (targetResults == null || targetResults.Count < 1)
            {
                ErrorMessage = errorBase + "target results file is empty";
                return false;
            }

            if (decoyResults == null || decoyResults.Count < 1)
            {
                ErrorMessage = errorBase + "decoy results file is empty";
                return false;
            }

            searchResults = new List<DatabaseSearchResultData>();
            searchResults.AddRange(decoyResults);
            searchResults.AddRange(targetResults);

            // Assign Result IDs, order by scan then EValue, then descending probability
            var resultID = 1;
            foreach (var item in searchResults.OrderBy(r => r.ScanNum)
                .ThenBy(r => r.EValue)
                .ThenByDescending(r => r.Probability))
            {
                item.ResultID = resultID++;
            }

            if (searchResults.Count == 0)
            {
                // NOTE: The DMS Analysis Manager looks for the text "No results found"
                // thus, do not change this message
                ErrorMessage = "No results found; cannot compute FDR Scores";
                return false;
            }

            return true;
        }
    }
}
