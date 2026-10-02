using System.ComponentModel;
using System.Text.Json.Serialization;
using SteelDeckFire.Core.Data;

namespace SteelDeckFire.Core.Models;

/// <summary>
/// Todos os dados de entrada de um painel de laje (zona de cálculo). Os atributos de
/// categoria/nome são usados diretamente pelo PropertyGrid da interface.
/// Convenção: L1 = vão das vigas internas sem proteção; L2 = dimensão perpendicular
/// (vão das vigas de perímetro que recebem as vigas internas).
/// </summary>
public class ProjectInput
{
    // ---------------- Identificação ----------------
    [Category("0. Identificação"), DisplayName("Projeto")]
    public string ProjectName { get; set; } = "Painel de laje – verificação em incêndio";

    [Category("0. Identificação"), DisplayName("Painel / zona")]
    public string PanelName { get; set; } = "Zona 1";

    [Category("0. Identificação"), DisplayName("Responsável")]
    public string Engineer { get; set; } = "";

    // ---------------- Geometria ----------------
    [Category("1. Geometria do painel"), DisplayName("L1 – vão das vigas internas (m)"),
     Description("Vão das vigas internas sem proteção. O painel é delimitado por vigas protegidas em todo o perímetro.")]
    public double L1 { get; set; } = 9.0;

    [Category("1. Geometria do painel"), DisplayName("L2 – dimensão perpendicular (m)"),
     Description("Dimensão do painel perpendicular às vigas internas (vão das vigas de perímetro que as recebem).")]
    public double L2 { get; set; } = 12.0;

    [Category("1. Geometria do painel"), DisplayName("Nº de vigas internas sem proteção"),
     Description("Vigas internas paralelas a L1, igualmente espaçadas. Espaçamento = L2/(n+1).")]
    public int UnprotectedBeams { get; set; } = 3;

    // ---------------- Fôrma e laje ----------------
    private string _deckName = "Metform MF-75";

    [Category("2. Fôrma e laje"), DisplayName("Telha-fôrma"), TypeConverter(typeof(DeckNameConverter)),
     RefreshProperties(RefreshProperties.All),
     Description("Selecione do catálogo ou 'Personalizada' para editar a geometria livremente.")]
    public string DeckName
    {
        get => _deckName;
        set
        {
            _deckName = value;
            var d = Catalogs.FindDeck(value);
            if (d is null) return;
            DeckH2 = d.H2; DeckL1 = d.L1; DeckL2 = d.L2; DeckL3 = d.L3;
            if (!d.Thicknesses.Contains(DeckThickness)) DeckThickness = d.Thicknesses[0];
        }
    }

    [Category("2. Fôrma e laje"), DisplayName("h2 – altura da fôrma (mm)")]
    public double DeckH2 { get; set; } = 75;

    [Category("2. Fôrma e laje"), DisplayName("l1 – nervura de concreto no topo (mm)"),
     Description("Largura da nervura de concreto no nível do topo da fôrma (EN 1994-1-2, Fig. D.1).")]
    public double DeckL1 { get; set; } = 155;

    [Category("2. Fôrma e laje"), DisplayName("l2 – nervura de concreto no fundo (mm)")]
    public double DeckL2 { get; set; } = 137;

    [Category("2. Fôrma e laje"), DisplayName("l3 – mesa superior da fôrma (mm)")]
    public double DeckL3 { get; set; } = 119;

    [Category("2. Fôrma e laje"), DisplayName("Espessura da fôrma (mm)")]
    public double DeckThickness { get; set; } = 0.80;

    [Category("2. Fôrma e laje"), DisplayName("ht – altura total da laje (mm)")]
    public double SlabThickness { get; set; } = 140;

    [Category("2. Fôrma e laje"), DisplayName("fck do concreto (MPa)")]
    public double Fck { get; set; } = 25;

    [Category("2. Fôrma e laje"), DisplayName("Peso específico do concreto (kN/m³)")]
    public double ConcreteWeight { get; set; } = 25;

    // ---------------- Tela ----------------
    private string _meshName = "Q196 (Ø5,0 c/10)";

    [Category("3. Tela soldada"), DisplayName("Tela"), TypeConverter(typeof(MeshNameConverter)),
     RefreshProperties(RefreshProperties.All)]
    public string MeshName
    {
        get => _meshName;
        set
        {
            _meshName = value;
            var m = Catalogs.FindMesh(value);
            if (m is null) return;
            AsAlongL2 = m.AsLongitudinal; AsAlongL1 = m.AsTransversal; MeshFy = m.Fy; MeshColdWorked = m.ColdWorked;
        }
    }

    [Category("3. Tela soldada"), DisplayName("As – barras paralelas a L2 (mm²/m)")]
    public double AsAlongL2 { get; set; } = 196;

    [Category("3. Tela soldada"), DisplayName("As – barras paralelas a L1 (mm²/m)")]
    public double AsAlongL1 { get; set; } = 196;

    [Category("3. Tela soldada"), DisplayName("fy da tela (MPa)")]
    public double MeshFy { get; set; } = 600;

    [Category("3. Tela soldada"), DisplayName("Fio trefilado a frio"),
     Description("Define a curva de redução da resistência com a temperatura (EN 1992-1-2, Tab. 3.2a).")]
    public bool MeshColdWorked { get; set; } = true;

    [Category("3. Tela soldada"), DisplayName("d – eixo da tela à face superior (mm)"),
     Description("Distância do eixo da tela à face superior (não exposta) da laje.")]
    public double MeshDepth { get; set; } = 30;

    [Category("3. Tela soldada"), DisplayName("Ductilidade classe B ou C"),
     Description("O método exige tela dúctil (EN 10080 classe B ou C). Telas CA-60 usuais não atendem.")]
    public bool MeshDuctile { get; set; } = false;

    // ---------------- Vigas internas ----------------
    private string _sectionName = "W 360 x 32,9";

    [Category("4. Vigas internas sem proteção"), DisplayName("Perfil"), TypeConverter(typeof(SectionNameConverter)),
     RefreshProperties(RefreshProperties.All)]
    public string SectionName
    {
        get => _sectionName;
        set
        {
            _sectionName = value;
            var s = Catalogs.FindSection(value);
            if (s is null) return;
            BeamH = s.H; BeamB = s.B; BeamTw = s.Tw; BeamTf = s.Tf; BeamArea = s.AreaCm2; BeamZx = s.ZxCm3;
        }
    }

    [Category("4. Vigas internas sem proteção"), DisplayName("d – altura (mm)")] public double BeamH { get; set; } = 349;
    [Category("4. Vigas internas sem proteção"), DisplayName("bf – largura da mesa (mm)")] public double BeamB { get; set; } = 127;
    [Category("4. Vigas internas sem proteção"), DisplayName("tw – alma (mm)")] public double BeamTw { get; set; } = 5.8;
    [Category("4. Vigas internas sem proteção"), DisplayName("tf – mesa (mm)")] public double BeamTf { get; set; } = 8.5;
    [Category("4. Vigas internas sem proteção"), DisplayName("A – área (cm²)")] public double BeamArea { get; set; } = 42.1;
    [Category("4. Vigas internas sem proteção"), DisplayName("Zx – módulo plástico (cm³)")] public double BeamZx { get; set; } = 547.6;
    [Category("4. Vigas internas sem proteção"), DisplayName("fy do perfil (MPa)")] public double BeamFy { get; set; } = 345;

    [Category("4. Vigas internas sem proteção"), DisplayName("Grau de interação a 20 °C"),
     Description("Grau de interação da viga mista em temperatura ambiente (0 a 1).")]
    public double ShearConnection { get; set; } = 1.0;

    [Category("4. Vigas internas sem proteção"), DisplayName("Temperatura imposta (°C, 0 = calcular)"),
     Description("Se zero, a temperatura da mesa inferior é calculada pelo método incremental da EN 1994-1-2.")]
    public double BeamTemperatureOverride { get; set; } = 0;

    // ---------------- Ações ----------------
    [Category("5. Ações"), DisplayName("Peso próprio da laje (kN/m², 0 = calcular)")]
    public double SlabSelfWeightOverride { get; set; } = 0;

    [Category("5. Ações"), DisplayName("Permanentes adicionais (kN/m²)"),
     Description("Revestimento, forro, instalações.")]
    public double AdditionalDead { get; set; } = 1.5;

    [Category("5. Ações"), DisplayName("Peso das vigas (kN/m²)")]
    public double BeamsSelfWeight { get; set; } = 0.3;

    [Category("5. Ações"), DisplayName("Sobrecarga de uso (kN/m²)")]
    public double LiveLoad { get; set; } = 3.0;

    [Category("5. Ações"), DisplayName("γg (combinação excepcional)"),
     Description("NBR 8681/NBR 14323: conferir valor aplicável. EN 1990: 1,0.")]
    public double GammaG { get; set; } = 1.2;

    [Category("5. Ações"), DisplayName("ψ da sobrecarga em incêndio"),
     Description("NBR 14323: 0,7·ψ2 (ex.: 0,7×0,4 = 0,28 para escritórios). EN 1990: ψ1 ou ψ2 conforme Anexo Nacional.")]
    public double PsiFire { get; set; } = 0.28;

    // ---------------- Incêndio ----------------
    [Category("6. Incêndio"), DisplayName("TRRF (min)"),
     Description("Tempo requerido de resistência ao fogo – incêndio-padrão ISO 834. Método validado até 120 min.")]
    public double FireTime { get; set; } = 60;

    [Category("6. Incêndio"), DisplayName("α – dilatação térmica do concreto (1/°C)")]
    public double ThermalAlpha { get; set; } = 1.2e-5;

    [Category("6. Incêndio"), DisplayName("Es – módulo da tela (MPa)")]
    public double MeshE { get; set; } = 210000;

    [JsonIgnore, Browsable(false)]
    public DeckProfile? Deck => Catalogs.FindDeck(DeckName);

    public ProjectInput Clone() => (ProjectInput)MemberwiseClone();

    /// <summary>Dados do exemplo resolvido do guia FRACOF (zona B, R60, tela ST 25C) – usados para validação.</summary>
    public static ProjectInput FracofExample() => new()
    {
        ProjectName = "Validação – exemplo FRACOF (Design Guide, zona B)",
        PanelName = "Zona B 9 × 12 m",
        L1 = 9, L2 = 12, UnprotectedBeams = 3,
        DeckName = "Cofraplus 60 (exemplo FRACOF)", SlabThickness = 130, Fck = 25,
        MeshName = "ST 25C (exemplo FRACOF)", MeshDepth = 30, MeshDuctile = true,
        SectionName = "IPE 400", BeamFy = 355, ShearConnection = 0.51,
        SlabSelfWeightOverride = 2.65, AdditionalDead = 0.7, BeamsSelfWeight = 0.5, LiveLoad = 5.0,
        GammaG = 1.0, PsiFire = 0.5, FireTime = 60,
    };
}

public sealed class DeckNameConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? c) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? c) => true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? c) =>
        new(Catalogs.Decks.Select(d => d.Name).Append(Catalogs.CustomName).ToArray());
}

public sealed class MeshNameConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? c) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? c) => true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? c) =>
        new(Catalogs.Meshes.Select(d => d.Name).Append(Catalogs.CustomName).ToArray());
}

public sealed class SectionNameConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? c) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? c) => true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? c) =>
        new(Catalogs.Sections.Select(d => d.Name).Append(Catalogs.CustomName).ToArray());
}
