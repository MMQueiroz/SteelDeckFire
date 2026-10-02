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

    /// <summary>
    /// Perfis laminados Gerdau, séries W e HP (ASTM A572 Gr. 50 / A992): d, bf, tw, tf, r em mm e massa nominal em kg/m.
    /// A e Zx são calculados pela geometria (ver <see cref="Calc.SectionProperties"/>). Conferir com a tabela vigente do fabricante.
    /// </summary>
    public static readonly IReadOnlyList<SteelSection> Sections = new List<SteelSection>
    {
        new("W 150 x 13,0", 148, 100, 4.3, 4.9, 10, 13.0),
        new("W 150 x 18,0", 153, 102, 5.8, 7.1, 10, 18.0),
        new("W 150 x 22,5 (H)", 152, 152, 5.8, 6.6, 10, 22.5),
        new("W 150 x 24,0", 160, 102, 6.6, 10.3, 10, 24.0),
        new("W 150 x 29,8 (H)", 157, 153, 6.6, 9.3, 10, 29.8),
        new("W 150 x 37,1 (H)", 162, 154, 8.1, 11.6, 10, 37.1),
        new("W 200 x 15,0", 200, 100, 4.3, 5.2, 10, 15.0),
        new("W 200 x 19,3", 203, 102, 5.8, 6.5, 10, 19.3),
        new("W 200 x 22,5", 206, 102, 6.2, 8.0, 10, 22.5),
        new("W 200 x 26,6", 207, 133, 5.8, 8.4, 10, 26.6),
        new("W 200 x 31,3", 210, 134, 6.4, 10.2, 10, 31.3),
        new("W 200 x 35,9 (H)", 201, 165, 6.2, 10.2, 10, 35.9),
        new("W 200 x 41,7 (H)", 205, 166, 7.2, 11.8, 10, 41.7),
        new("W 200 x 46,1 (H)", 203, 203, 7.2, 11.0, 10, 46.1),
        new("W 200 x 52,0 (H)", 206, 204, 7.9, 12.6, 10, 52.0),
        new("W 200 x 59,0 (H)", 210, 205, 9.1, 14.2, 10, 59.0),
        new("W 200 x 71,0 (H)", 216, 206, 10.2, 17.4, 10, 71.0),
        new("W 200 x 86,0 (H)", 222, 209, 13.0, 20.6, 10, 86.0),
        new("W 250 x 17,9", 251, 101, 4.8, 5.3, 10, 17.9),
        new("W 250 x 22,3", 254, 102, 5.8, 6.9, 10, 22.3),
        new("W 250 x 25,3", 257, 102, 6.1, 8.4, 10, 25.3),
        new("W 250 x 28,4", 260, 102, 6.4, 10.0, 10, 28.4),
        new("W 250 x 32,7", 258, 146, 6.1, 9.1, 10, 32.7),
        new("W 250 x 38,5", 262, 147, 6.6, 11.2, 10, 38.5),
        new("W 250 x 44,8", 266, 148, 7.6, 13.0, 10, 44.8),
        new("W 250 x 73,0 (H)", 253, 254, 8.6, 14.2, 12, 73.0),
        new("W 250 x 80,0 (H)", 256, 255, 9.4, 15.6, 12, 80.0),
        new("W 250 x 89,0 (H)", 260, 256, 10.7, 17.3, 12, 89.0),
        new("W 250 x 101,0 (H)", 264, 257, 11.9, 19.6, 12, 101.0),
        new("W 250 x 115,0 (H)", 269, 259, 13.5, 22.1, 12, 115.0),
        new("W 310 x 21,0", 303, 101, 5.1, 5.7, 10, 21.0),
        new("W 310 x 23,8", 305, 101, 5.6, 6.7, 10, 23.8),
        new("W 310 x 28,3", 309, 102, 6.0, 8.9, 10, 28.3),
        new("W 310 x 32,7", 313, 102, 6.6, 10.8, 10, 32.7),
        new("W 310 x 38,7", 310, 165, 5.8, 9.7, 10, 38.7),
        new("W 310 x 44,5", 313, 166, 6.6, 11.2, 10, 44.5),
        new("W 310 x 52,0", 317, 167, 7.6, 13.2, 10, 52.0),
        new("W 310 x 79,0 (H)", 306, 254, 8.8, 14.6, 16, 79.0),
        new("W 310 x 97,0 (H)", 308, 305, 9.9, 15.4, 16, 97.0),
        new("W 310 x 107,0 (H)", 311, 306, 10.9, 17.0, 16, 107.0),
        new("W 310 x 117,0 (H)", 314, 307, 11.9, 18.7, 16, 117.0),
        new("W 360 x 32,9", 349, 127, 5.8, 8.5, 12, 32.9),
        new("W 360 x 39,0", 353, 128, 6.5, 10.7, 12, 39.0),
        new("W 360 x 44,0", 352, 171, 6.9, 9.8, 12, 44.0),
        new("W 360 x 51,0", 355, 171, 7.2, 11.6, 12, 51.0),
        new("W 360 x 57,8", 358, 172, 7.9, 13.1, 12, 57.8),
        new("W 360 x 64,0", 347, 203, 7.7, 13.5, 12, 64.0),
        new("W 360 x 72,0", 350, 204, 8.6, 15.1, 12, 72.0),
        new("W 360 x 79,0", 354, 205, 9.4, 16.8, 12, 79.0),
        new("W 360 x 91,0 (H)", 353, 254, 9.5, 16.4, 16, 91.0),
        new("W 360 x 101,0 (H)", 357, 255, 10.5, 18.3, 16, 101.0),
        new("W 360 x 110,0 (H)", 360, 256, 11.4, 19.9, 16, 110.0),
        new("W 360 x 122,0 (H)", 363, 257, 13.0, 21.7, 16, 122.0),
        new("W 410 x 38,8", 399, 140, 6.4, 8.8, 12, 38.8),
        new("W 410 x 46,1", 403, 140, 7.0, 11.2, 12, 46.1),
        new("W 410 x 53,0", 403, 177, 7.5, 10.9, 12, 53.0),
        new("W 410 x 60,0", 407, 178, 7.7, 12.8, 12, 60.0),
        new("W 410 x 67,0", 410, 179, 8.8, 14.4, 12, 67.0),
        new("W 410 x 75,0", 413, 180, 9.7, 16.0, 12, 75.0),
        new("W 410 x 85,0", 417, 181, 10.9, 18.2, 12, 85.0),
        new("W 460 x 52,0", 450, 152, 7.6, 10.8, 12, 52.0),
        new("W 460 x 60,0", 455, 153, 8.0, 13.3, 12, 60.0),
        new("W 460 x 68,0", 459, 154, 9.1, 15.4, 12, 68.0),
        new("W 460 x 74,0", 457, 190, 9.0, 14.5, 12, 74.0),
        new("W 460 x 82,0", 460, 191, 9.9, 16.0, 12, 82.0),
        new("W 460 x 89,0", 463, 192, 10.5, 17.7, 12, 89.0),
        new("W 460 x 97,0", 466, 193, 11.4, 19.0, 12, 97.0),
        new("W 460 x 106,0", 469, 194, 12.6, 20.6, 12, 106.0),
        new("W 530 x 66,0", 525, 165, 8.9, 11.4, 12, 66.0),
        new("W 530 x 72,0", 524, 207, 9.0, 10.9, 12, 72.0),
        new("W 530 x 74,0", 529, 166, 9.7, 13.6, 12, 74.0),
        new("W 530 x 82,0", 528, 209, 9.5, 13.3, 12, 82.0),
        new("W 530 x 85,0", 535, 166, 10.3, 16.5, 12, 85.0),
        new("W 530 x 92,0", 533, 209, 10.2, 15.6, 12, 92.0),
        new("W 530 x 101,0", 537, 210, 10.9, 17.4, 12, 101.0),
        new("W 530 x 109,0", 539, 211, 11.6, 18.8, 12, 109.0),
        new("W 610 x 101,0", 603, 228, 10.5, 14.9, 16, 101.0),
        new("W 610 x 113,0", 608, 228, 11.2, 17.3, 16, 113.0),
        new("W 610 x 125,0", 612, 229, 11.9, 19.6, 16, 125.0),
        new("W 610 x 140,0", 617, 230, 13.1, 22.2, 16, 140.0),
        new("W 610 x 155,0", 611, 324, 12.7, 19.0, 16, 155.0),
        new("W 610 x 174,0", 616, 325, 14.0, 21.6, 16, 174.0),
        new("HP 200 x 53,0", 204, 207, 11.3, 11.3, 10, 53.0),
        new("HP 250 x 62,0", 246, 256, 10.5, 10.7, 12, 62.0),
        new("HP 250 x 85,0", 254, 260, 14.4, 14.4, 12, 85.0),
        new("HP 310 x 79,0", 299, 306, 11.0, 11.0, 16, 79.0),
        new("HP 310 x 93,0", 303, 308, 13.1, 13.1, 16, 93.0),
        new("HP 310 x 110,0", 308, 310, 15.4, 15.5, 16, 110.0),
        new("HP 310 x 125,0", 312, 312, 17.4, 17.4, 16, 125.0),
        new("IPE 400", 400, 180, 8.6, 13.5, 21, 66.3),
    };

    public static DeckProfile? FindDeck(string name) => Decks.FirstOrDefault(d => d.Name == name);
    public static MeshType? FindMesh(string name) => Meshes.FirstOrDefault(m => m.Name == name);
    public static SteelSection? FindSection(string name) => Sections.FirstOrDefault(s => s.Name == name);
}
