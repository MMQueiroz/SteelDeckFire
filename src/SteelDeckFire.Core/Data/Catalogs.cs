using SteelDeckFire.Core.Models;

namespace SteelDeckFire.Core.Data;

/// <summary>
/// Catálogos embutidos. IMPORTANTE: as dimensões das nervuras (l1, l2, l3) das fôrmas Metform
/// são aproximadas e devem ser conferidas com o catálogo/manual técnico vigente do fabricante.
/// Altura e largura útil são as nominais do fabricante. As dimensões podem ser editadas na interface
/// (escolha "Personalizada") sem alterar este arquivo.
/// </summary>
public static class Catalogs
{
    public const string CustomName = "Personalizada";

    public static readonly IReadOnlyList<DeckProfile> Decks = new List<DeckProfile>
    {
        new("Metform MF-50", H2: 50, L1: 172, L2: 152, L3: 133,
            Thicknesses: new[] { 0.80, 0.95, 1.25 }, CoverWidth: 915,
            Note: "Altura 50 mm, largura útil 915 mm (3 ondas). l1/l2/l3 aproximados – conferir catálogo Metform."),
        new("Metform MF-75", H2: 75, L1: 155, L2: 137, L3: 119,
            Thicknesses: new[] { 0.80, 0.95, 1.25 }, CoverWidth: 820,
            Note: "Altura 75 mm, largura útil 820 mm (passo 274 mm). l1/l2/l3 aproximados – conferir catálogo Metform."),
        new("Cofraplus 60 (exemplo FRACOF)", H2: 58, L1: 101, L2: 62, L3: 106,
            Thicknesses: new[] { 0.75, 0.88, 1.00 }, CoverWidth: 1035,
            Note: "Geometria usada no exemplo resolvido do guia FRACOF (validação)."),
    };

    /// <summary>Telas soldadas Gerdau série Q (CA-60, fio trefilado) e telas do exemplo FRACOF.</summary>
    public static readonly IReadOnlyList<MeshType> Meshes = new List<MeshType>
    {
        new("Q61 (Ø3,4 c/15)",  61,  61, 600, true, "CA-60"),
        new("Q75 (Ø3,8 c/15)",  75,  75, 600, true, "CA-60"),
        new("Q92 (Ø4,2 c/15)",  92,  92, 600, true, "CA-60"),
        new("Q113 (Ø3,8 c/10)", 113, 113, 600, true, "CA-60"),
        new("Q138 (Ø4,2 c/10)", 138, 138, 600, true, "CA-60"),
        new("Q159 (Ø4,5 c/10)", 159, 159, 600, true, "CA-60"),
        new("Q196 (Ø5,0 c/10)", 196, 196, 600, true, "CA-60"),
        new("Q246 (Ø5,6 c/10)", 246, 246, 600, true, "CA-60"),
        new("Q283 (Ø6,0 c/10)", 283, 283, 600, true, "CA-60"),
        new("Q335 (Ø8,0 c/15)", 335, 335, 600, true, "CA-60"),
        new("Q396 (Ø7,1 c/10)", 396, 396, 600, true, "CA-60"),
        new("Q503 (Ø8,0 c/10)", 503, 503, 600, true, "CA-60"),
        new("ST 15C (exemplo FRACOF)", 142, 142, 500, false, "Classe B, EN 10080"),
        new("ST 25C (exemplo FRACOF)", 257, 257, 500, false, "Classe B, EN 10080"),
    };

    /// <summary>Perfis laminados (valores de referência – conferir com a tabela do fabricante).</summary>
    public static readonly IReadOnlyList<SteelSection> Sections = new List<SteelSection>
    {
        new("W 200 x 15,0", 200, 100, 4.3, 5.2, 19.4, 147.9),
        new("W 200 x 19,3", 203, 102, 5.8, 6.5, 25.1, 190.6),
        new("W 250 x 17,9", 251, 101, 4.8, 5.3, 23.1, 211.0),
        new("W 250 x 25,3", 257, 102, 6.1, 8.4, 32.6, 311.1),
        new("W 310 x 21,0", 303, 101, 5.1, 5.7, 27.2, 291.9),
        new("W 310 x 28,3", 309, 102, 6.0, 8.9, 36.5, 412.0),
        new("W 310 x 32,7", 313, 102, 6.6, 10.8, 42.1, 485.3),
        new("W 360 x 32,9", 349, 127, 5.8, 8.5, 42.1, 547.6),
        new("W 360 x 39,0", 353, 128, 6.5, 10.7, 50.2, 667.7),
        new("W 410 x 38,8", 399, 140, 6.4, 8.8, 50.3, 736.8),
        new("W 410 x 46,1", 403, 140, 7.0, 11.2, 59.2, 891.1),
        new("W 460 x 52,0", 450, 152, 7.6, 10.8, 66.6, 1095.9),
        new("W 530 x 66,0", 525, 165, 8.9, 11.4, 83.6, 1558.0),
        new("IPE 400", 400, 180, 8.6, 13.5, 84.46, 1307.0),
    };

    public static DeckProfile? FindDeck(string name) => Decks.FirstOrDefault(d => d.Name == name);
    public static MeshType? FindMesh(string name) => Meshes.FirstOrDefault(m => m.Name == name);
    public static SteelSection? FindSection(string name) => Sections.FirstOrDefault(s => s.Name == name);
}
