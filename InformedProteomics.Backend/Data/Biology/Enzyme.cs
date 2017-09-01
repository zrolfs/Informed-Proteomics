using System.Linq;

namespace InformedProteomics.Backend.Data.Biology
{
    /// <summary>
    /// Enzyme, specifically digestion enzymes
    /// </summary>
    public class Enzyme
    {
        private Enzyme(string name, string residues, bool isNTerm, string description, string psiCvAccession)
        {
            _name = name;
            _residues = residues.ToCharArray();
            _isNTerm = isNTerm;
            _description = description;
            _psiCvAccession = psiCvAccession;
        }

        private readonly string _name;
        private readonly char[] _residues;
        private readonly bool _isNTerm;
        private readonly string _description;
        private readonly string _psiCvAccession;

        /// <summary>
        /// Name of the enzyme
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Residues cleaved by the enzyme
        /// </summary>
        public char[] Residues
        {
            get { return _residues; }
        }

        /// <summary>
        /// If the enzyme affects the N-Terminus
        /// </summary>
        public bool IsNTerm
        {
            get { return _isNTerm; }
        }

        /// <summary>
        /// Description of the enzyme
        /// </summary>
        public string Description
        {
            get { return _description; }
        }

        /// <summary>
        /// PSI-MS CV Accession for the enzyme
        /// </summary>
        public string PsiCvAccession
        {
            get { return _psiCvAccession; }
        }

        /// <summary>
        /// Test if the enzyme will cleave at <paramref name="residue"/>
        /// </summary>
        /// <param name="residue"></param>
        /// <returns></returns>
        public bool IsCleavable(char residue)
        {
            if (_residues.Length == 0)
                return true;
            return _residues.Any(r => r == residue);
        }

        /// <summary>
        /// Unspecific cleavage
        /// </summary>
        public static readonly Enzyme UnspecificCleavage = new Enzyme("UnspecificCleavage", "", false, "unspecific cleavage", "MS:1001956");

        /// <summary>
        /// Trypsin enzyme
        /// </summary>
        public static readonly Enzyme Trypsin = new Enzyme("Trypsin", "KR", false, "Trypsin", "MS:1001251");

        /// <summary>
        /// Chymotrypsin enzyme
        /// </summary>
        public static readonly Enzyme Chymotrypsin = new Enzyme("Chymotrypsin", "FYWL", false, "Chymotrypsin", "MS:1001306");

        /// <summary>
        /// LysC enzyme
        /// </summary>
        public static readonly Enzyme LysC = new Enzyme("LysC", "K", false, "Lys-C", "MS:1001309");

        /// <summary>
        /// LysN enzyme
        /// </summary>
        public static readonly Enzyme LysN = new Enzyme("LysN", "K", true, "Lys-N", null);

        /// <summary>
        /// GluC enzyme
        /// </summary>
        public static readonly Enzyme GluC = new Enzyme("GluC","E",false, "Glu-C", "MS:1001917");

        /// <summary>
        /// ArgC enzyme
        /// </summary>
        public static readonly Enzyme ArgC = new Enzyme("ArgC","R",false, "Arg-C", "MS:1001303");

        /// <summary>
        /// AspN enzyme
        /// </summary>
        public static readonly Enzyme AspN = new Enzyme("AspN","D",true, "Asp-N", "MS:1001304");

        /// <summary>
        /// Alp enzyme
        /// </summary>
        public static readonly Enzyme Alp = new Enzyme("aLP", "", false, "alphaLP", null);

        /// <summary>
        /// No cleavage enzyme
        /// </summary>
        public static readonly Enzyme NoCleavage = new Enzyme("NoCleavage", "", false, "no cleavage", "MS:1001955");
    }
}
