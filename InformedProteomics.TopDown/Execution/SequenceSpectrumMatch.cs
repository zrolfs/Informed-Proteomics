﻿using System;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Database;

namespace InformedProteomics.TopDown.Execution
{
    public class SequenceSpectrumMatch: IComparable<SequenceSpectrumMatch>
    {
        public SequenceSpectrumMatch(string sequence, char pre, char post, int scanNum, long offset, int numNTermCleavages, ModificationCombination modifications, Ion ion, double score)
        {
            Sequence = sequence;
            Pre = pre == FastaDatabase.Delimiter ? '-' : pre;
            Post = post == FastaDatabase.Delimiter ? '-' : post;
            ScanNum = scanNum;
            Offset = offset;
            NumNTermCleavages = numNTermCleavages;
            Modifications = modifications;
            Ion = ion;
            Score = score;
        }

        public string Sequence { get; private set; }
        public char Pre { get; private set; }
        public char Post { get; private set; }
        public int ScanNum { get; private set; }
        public long Offset { get; private set; }
        public int NumNTermCleavages { get; private set; }
        public ModificationCombination Modifications { get; private set; }
        public Ion Ion { get; private set; }
        public double Score { get; private set; }

        public AminoAcid NTerm
        {
            get { return Pre == FastaDatabase.Delimiter ? AminoAcid.ProteinNTerm : AminoAcid.PeptideNTerm; }
        }

        public AminoAcid CTerm
        {
            get { return Post == FastaDatabase.Delimiter ? AminoAcid.ProteinCTerm : AminoAcid.PeptideCTerm; }
        }

        public int CompareTo(SequenceSpectrumMatch other)
        {
            return Score.CompareTo(other.Score);
        }
    }
}