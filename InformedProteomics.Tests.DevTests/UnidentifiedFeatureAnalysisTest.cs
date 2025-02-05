﻿using System;
using System.Reflection;
using InformedProteomics.Tests.Base;
using InformedProteomics.TopDown.Quantification;
using NUnit.Framework;

namespace InformedProteomics.Tests.DevTests
{
    [TestFixture]
    public class UnidentifiedFeatureAnalysisTest
    {
        [Test]
        [Category("Local_Testing")]
        [Ignore("Missing files")]
        public void TestUnidentifiedFeatureAnalysis()
        {
            var methodName = MethodBase.GetCurrentMethod().Name;
            Utils.ShowStarting(methodName);

            const string crossTabFile = @"C:\Users\mend645\Desktop\TopDown_Analyis\CPTAC_Intact_CompRef_Analysis.txt";
            const string outputFolder = @"C:\Users\mend645\Desktop\TopDown_Analyis\";
            var rawFiles = new[]
            {
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR32A_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR32B_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR32C_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR32D_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR32E_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR32F_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR32G_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR33A_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR33B_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR33C_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR33D_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR33E_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR33F_24Aug15_Bane_15-02-06-RZ.pbf",
                @"\\protoapps\UserData\Jungkap\CompRef\raw\CPTAC_Intact_CR33G_24Aug15_Bane_15-02-06-RZ.pbf"
            };
            const string databaseFile = @"\\protoapps\UserData\Jungkap\CompRef\db\H_sapiens_M_musculus_Trypsin_NCBI_Build37_2011-12-02.fasta";

            var unFeatureAnalyzer = new UnidentifiedFeatureAnalysis(rawFiles, crossTabFile, databaseFile);
            Console.WriteLine("Filtering Features.............");
            unFeatureAnalyzer.FilterFeatures(outputFolder);
            unFeatureAnalyzer.DoAnalysis();
            unFeatureAnalyzer.PrintAnalysisToFile(outputFolder);
        }
    }
}
