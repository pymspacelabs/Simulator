namespace SimulatorML.Models;

public static class CelestialTargetCatalog
{
    public static readonly IReadOnlyList<CelestialBodyProfile> SupportedBodies =
    [
        new(
            CelestialTargetId.ISS, "iss", "ISS (LEO)", "ISS (LEO)",
            "International Space Station (ISS — LEO 414 km)", "Estação Espacial Internacional (ISS — LEO 414 km)",
            "-125544", 414.2, MathF.Log10(414.2f), 1.000f, 1.000f, 0.85f, 0.15f,
            8.69f, 11.19f, 0.0035f, 0.0025f, 9800f, 0.25f, 0.20f, 0.30f, 16.0f, 6, 2200f,
            ["Microgravity cephalic fluid shift & SANS neuro-ocular pressure", "1.5%/mo bone mineral loss without ARED/Centrifuge", "South Atlantic Anomaly proton passes & atomic oxygen (ATOX) erosion"],
            ["Deslocamento cefálico de fluidos em microgravidade e síndrome ocular SANS", "Perda óssea de 1,5%/mês sem exercício ARED/Centrífuga", "Prótons na Anomalia do Atlântico Sul e erosão por oxigênio atômico (ATOX)"]
        ),
        new(
            CelestialTargetId.Moon, "moon", "Moon (Luna)", "Lua (Luna)",
            "Moon — Lunar Gateway NRHO & Artemis South Pole Outpost", "Lua — Órbita Lunar NRHO & Outpost Polo Sul Artemis",
            "301", 384400.0, MathF.Log10(384400f), 1.000f, 1.000f, 1.15f, 0.45f,
            1.62f, 2.38f, 0.0085f, 0.0075f, 13850f, 3.50f, 3.50f, 0.34f, 24.0f, 4, 2400f,
            ["Razor-sharp electrostatic silicate regolith abrasion on xEMU suits", "Unshielded Solar Particle Events (SPE) outside Earth's magnetosphere", "Extreme 14-day lunar night cryogenic swings (-180°C to +120°C)"],
            ["Abrasão severa por regolito de silicato eletrostático nos trajes xEMU", "Eventos de Partículas Solares (SPE) fora da magnetosfera terrestre", "Noite lunar criogênica de 14 dias (-180°C a +120°C)"]
        ),
        new(
            CelestialTargetId.Mars, "mars", "Mars", "Marte",
            "Mars — Areocentric Orbit & Surface ISRU Outpost", "Marte — Órbita Areocêntrica & Outpost de Superfície ISRU",
            "499", 78340000.0, MathF.Log10(78340000f), 1.524f, 0.431f, 1.55f, 0.68f,
            3.71f, 5.03f, 0.0110f, 0.0095f, 17900f, 180.0f, 180.0f, 0.36f, 30.0f, 4, 2800f,
            ["Toxic calcium/magnesium perchlorates (ClO4-) in Martian dust", "6-month interplanetary Galactic Cosmic Ray (GCR) exposure", "Global dust storms reducing solar flux (0.43x baseline) & abrading suits"],
            ["Percloratos tóxicos de cálcio/magnésio (ClO4-) na poeira marciana", "Exposição de 6 meses a Raios Cósmicos Galácticos (GCR) no trânsito", "Tempestades globais de poeira reduzindo luz solar (0,43x) e desgastando trajes"]
        )
    ];

    public static CelestialBodyProfile GetById(CelestialTargetId id) =>
        SupportedBodies.FirstOrDefault(b => b.Id == id) ?? SupportedBodies[2];
}
