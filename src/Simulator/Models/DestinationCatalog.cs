namespace Simulator.Models;

public static class DestinationCatalog
{
    private static readonly List<DestinationProfile> _profiles =
    [
        new(1, "ISS", "ISS (LEO)", "ISS (LEO)",
            "Estação Espacial Internacional (ISS — LEO 414 km)", "International Space Station (ISS — LEO 414 km)",
            "-125544", 414.2, 1.000f, 1.000f, 0.85f, 0.15f, 8.69f, 11.19f, 0.0035f, 9800f, 0.25f, 0.20f, 0.30f, 16.0f, 6, 2200f,
            ["Deslocamento cefálico de fluidos em microgravidade e síndrome neuro-ocular (SANS)", "Perda óssea de 1,5%/mês sem exercício resistido ARED ou Centrífuga", "Passagens de prótons na Anomalia do Atlântico Sul e erosão por oxigênio atômico (ATOX)"],
            ["Microgravity cephalic fluid shift & SANS neuro-ocular syndrome", "1.5%/mo bone mineral loss without ARED exercise or Centrifuge", "South Atlantic Anomaly trapped proton passes & atomic oxygen (ATOX) erosion"]),

        new(2, "Moon", "Lua (Luna)", "Moon (Luna)",
            "Lua — Órbita Lunar NRHO & Outpost Polo Sul Artemis", "Moon — Lunar Gateway NRHO & Artemis South Pole Outpost",
            "301", 384400.0, 1.000f, 1.000f, 1.15f, 0.45f, 1.62f, 2.38f, 0.0085f, 13850f, 3.50f, 3.50f, 0.34f, 24.0f, 4, 2400f,
            ["Abrasão severa por regolito de silicato eletrostático nos trajes xEMU", "Eventos de Partículas Solares (SPE) fora da magnetosfera terrestre", "Noite lunar criogênica de 14 dias (-180°C a +120°C)"],
            ["Razor-sharp electrostatic silicate regolith abrasion on xEMU suits", "Unshielded Solar Particle Events (SPE) outside Earth's magnetosphere", "14-day cryogenic lunar night (-180°C to +120°C)"]),

        new(3, "Mars", "Marte", "Mars",
            "Marte — Órbita Areocêntrica & Outpost Superficial ISRU", "Mars — Areocentric Orbit & Surface ISRU Outpost",
            "499", 78340000.0, 1.524f, 0.431f, 1.55f, 0.68f, 3.71f, 5.03f, 0.0110f, 17900f, 180.0f, 180.0f, 0.36f, 30.0f, 4, 2800f,
            ["Percloratos tóxicos de cálcio/magnésio (ClO4-) na poeira marciana", "Exposição de 6 meses a Raios Cósmicos Galácticos (GCR) no trânsito", "Tempestades globais de poeira reduzindo luz solar (0,43x) e desgastando trajes"],
            ["Toxic calcium/magnesium perchlorates (ClO4-) in Martian dust", "6-month interplanetary Galactic Cosmic Ray (GCR) exposure", "Global dust storms reducing solar flux (0.43x) & abrading suits"])
    ];

    public static DestinationProfile GetProfile(string key) =>
        _profiles.FirstOrDefault(p => p.IdKey.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? _profiles[0];
}
