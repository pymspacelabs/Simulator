# 🚀 Simulator interactive tool

**Simulator** is an interactive Bio-Ship (*Nave-Bioma*) mission simulator powered by a **240,000-sample / 32-feature Quad-Ensemble `ML.NET` (`FastTree`)** model and **8 public NASA APIs**, designed to evaluate closed-loop life support, crew biology, and round-trip propellant autonomy for missions to the **ISS**, **Moon**, and **Mars**.

Below is the complete bilingual glossary (**pt-BR** & **en-US**) of every acronym and technical abbreviation used across the **Simulator** engine, decks, and telemetry UI.

---

## Destinations, Orbits & Space Agencies / Destinos, Órbitas e Agências Espaciais

| Sigla / Acronym | Nome Completo / Full Name | Descrição (pt-BR) | Description (en-US) |
| :--- | :--- | :--- | :--- |
| **ISS** | International Space Station | Estação Espacial Internacional em órbita terrestre baixa (~415 km), um dos três destinos operacionais do simulador. | International Space Station in low Earth orbit (~415 km), one of the simulator's three operational destinations. |
| **LEO** | Low Earth Orbit | Órbita Terrestre Baixa, região orbital onde opera a ISS e ocorrem os testes iniciais de suporte à vida. | Low Earth Orbit, the orbital regime where the ISS operates and early life-support tests take place. |
| **NRHO** | Near-Rectilinear Halo Orbit | Órbita Halo Quase-Retilínea ao redor da Lua, utilizada como referência de trajetória cislunar para missões Artemis. | Near-Rectilinear Halo Orbit around the Moon, used as the cislunar trajectory reference for Artemis missions. |
| **NASA** | National Aeronautics and Space Administration | Agência espacial norte-americana fornecedora das APIs públicas, padrões biológicos e especificações de engenharia do modelo. | U.S. space agency providing the public APIs, biological standards, and engineering specifications used by the model. |
| **MSFC** | Marshall Space Flight Center | Centro da NASA (Huntsville, AL) responsável pelo desenvolvimento e teste dos sistemas regenerativos ECLSS. | NASA center (Huntsville, AL) responsible for developing and testing regenerative ECLSS hardware. |
| **JSC** | Johnson Space Center | Centro Espacial Johnson da NASA, referência em trajes espaciais (xEMU/Z-2), fisiologia e protocolos médicos da tripulação. | NASA Johnson Space Center, reference hub for spacesuits (xEMU/Z-2), crew physiology, and medical protocols. |
| **JPL** | Jet Propulsion Laboratory | Laboratório de Propulsão a Jato da NASA, responsável pelas efemérides planetárias (Horizons) e comunicações ópticas (DSOC). | NASA Jet Propulsion Laboratory, responsible for planetary ephemerides (Horizons) and optical communications (DSOC). |
| **LANL** | Los Alamos National Laboratory | Laboratório Nacional de Los Alamos, parceiro da NASA no reator de fissão nuclear espacial Kilopower (KRUSTY). | Los Alamos National Laboratory, NASA partner on the Kilopower (KRUSTY) space fission reactor. |
| **NORAD** | North American Aerospace Defense Command | Comando responsável pela catalogação orbital de objetos espaciais (ex.: ID `25544` da ISS para dados TLE). | Command responsible for cataloging orbital space objects (e.g., ISS catalog ID `25544` for TLE tracking). |
| **ISRU** | In-Situ Resource Utilization | Utilização de recursos locais (como gelo lunar ou $\text{CO}_2$ marciano) para produzir água, oxigênio e propelente. | In-Situ Resource Utilization of local planetary resources (such as lunar ice or Martian $\text{CO}_2$) to produce water, oxygen, and propellant. |

---

## NASA APIs, Telemetry & Standards / APIs da NASA, Telemetria e Padrões

| Sigla / Acronym | Nome Completo / Full Name | Descrição (pt-BR) | Description (en-US) |
| :--- | :--- | :--- | :--- |
| **API** | Application Programming Interface | Interface de programação usada pelo simulador para consultar telemetria pública oficial da NASA. | Programming interface used by the simulator to query official public NASA telemetry. |
| **REST** | Representational State Transfer | Estilo de arquitetura HTTP utilizado nas chamadas às 8 APIs públicas da NASA. | HTTP architectural style used for querying the 8 public NASA APIs. |
| **DONKI** | Space Weather Database Of Notifications, Knowledge, Information | Base de clima espacial da NASA usada para monitorar explosões solares e tempestades de radiação em tempo real. | NASA space weather database used to monitor solar flares and radiation storms in real time. |
| **FLR** | Solar Flare | Classe de evento do DONKI que registra erupções solares e impactos de fluxo eletromagnético na nave. | DONKI event class recording solar flares and electromagnetic flux impacts on the spacecraft. |
| **CME** | Coronal Mass Ejection | Ejeção de Massa Coronal monitorada pelo DONKI para calcular picos de radiação no trajeto Terra–Lua–Marte. | Coronal Mass Ejection tracked via DONKI to compute radiation spikes along the Earth–Moon–Mars transit. |
| **SEP / SPE** | Solar Energetic Particle / Solar Particle Event | Eventos de partículas energéticas solares que elevam o risco de radiação ionizante sobre a tripulação e o bioma. | Solar energetic particle events that increase ionizing radiation risk for the crew and biome. |
| **RBE** | Radiation Belt Enhancement | Intensificação dos cinturões de radiação (monitorada no DONKI) que afeta a travessia orbital em LEO. | Radiation Belt Enhancement (tracked via DONKI) affecting orbital transit through LEO. |
| **OSDR** | Open Science Data Repository | Repositório científico da NASA (GeneLab) com estudos sobre perda óssea, atrofia muscular, radiação e microalgas. | NASA Open Science Data Repository (GeneLab) providing studies on bone loss, muscle atrophy, radiation, and microalgae. |
| **OSD-521** | OSDR Study 521 (Photobioreactor) | Experimento biológico com microalgas *Chlorella* em fotobiorreator a bordo da ISS usado para calibrar o bioma da nave. | Biological experiment with *Chlorella* microalgae in a photobioreactor aboard the ISS used to calibrate the ship's biome. |
| **LSR** | Life Support Rack | Rack de suporte à vida da ESA/NASA na ISS onde o experimento de fotobiorreator (`PBR@LSR`) foi operado. | ESA/NASA Life Support Rack on the ISS where the photobioreactor experiment (`PBR@LSR`) was operated. |
| **NTRS** | NASA Technical Reports Server | Servidor de relatórios técnicos da NASA que fundamenta as equações de balanço de massa e economia de propelente. | NASA Technical Reports Server grounding the simulator's mass-balance and propellant-economy equations. |
| **TLE** | Two-Line Element Set | Formato orbital padrão usado para obter a altitude atualizada da ISS (~414,8 km) via API. | Standard orbital element format used to fetch the live altitude of the ISS (~414.8 km) via API. |
| **REMS** | Rover Environmental Monitoring Station | Estação meteorológica do rover Curiosity em Marte, referência complementar de clima marciano junto à API InSight. | Weather station on the Mars Curiosity rover, complementing the InSight Mars Weather API. |
| **SWIM** | Subsurface Water Ice Mapping | Projeto da NASA para mapeamento de gelo de água subsuperficial em Marte usado na carta de extração de água. | NASA Mars Subsurface Water Ice Mapping project referenced by the Martian water-extraction deck card. |
| **BVAD** | Baseline Values and Assumptions Document | Documento de referência da NASA com taxas metabólicas humanas padrão ($\text{O}_2$, $\text{CO}_2$, água e alimento seco por tripulante/dia). | NASA reference document defining standard human metabolic rates ($\text{O}_2$, $\text{CO}_2$, water, and dry food per crewmember/day). |
| **NASA-STD-3001** | NASA Space Flight Human-System Standard | Norma médica e ergonômica da NASA que define limites de exposição à radiação, gravidade artificial e saúde espacial. | NASA human-system medical and ergonomic standard governing radiation limits, artificial gravity, and crew health. |
| **MISSE** | Materials International Space Station Experiment | Experimento na ISS que testa resistência de materiais e revestimentos contra oxigênio atômico (ATOX) e UV. | ISS experiment testing material and coating durability against atomic oxygen (ATOX) and UV exposure. |
| **DSOC** | Deep Space Optical Communications | Sistema de comunicação a laser de alta largura de banda (Gbps) para telemetria médica e controle em espaço profundo. | High-bandwidth laser communications system (Gbps) for deep-space medical telemetry and spacecraft control. |

---

## ECLSS & Life-Support Systems / ECLSS e Sistemas de Suporte à Vida

| Sigla / Acronym | Nome Completo / Full Name | Descrição (pt-BR) | Description (en-US) |
| :--- | :--- | :--- | :--- |
| **ECLSS** | Environmental Control and Life Support System | Sistema de Controle Ambiental e Suporte à Vida que fecha os ciclos de ar, água e resíduos da nave-bioma. | Environmental Control and Life Support System closing the air, water, and waste loops of the Bio-Ship. |
| **WRS** | Water Recovery System | Sistema de Recuperação de Água do MSFC que integra o WPA e o UPA para reciclar até ~93,5% da água da cabine. | MSFC Water Recovery System integrating the WPA and UPA to recycle up to ~93.5% of cabin water. |
| **WPA** | Water Processor Assembly | Conjunto processador que filtra e purifica condensado atmosférico e destilado de urina em água potável. | Water Processor Assembly that filters and purifies cabin humidity condensate and urine distillate into potable water. |
| **UPA** | Urine Processor Assembly | Conjunto que recupera água da urina por destilação a vácuo, enviando salmoura residual para o BPA. | Urine Processor Assembly recovering water from urine via vacuum distillation and routing residual brine to the BPA. |
| **VCD** | Vapor Compression Distillation | Tecnologia de destilação por compressão de vapor usada dentro do UPA para separar água limpa de sais. | Vapor Compression Distillation technology used inside the UPA to separate clean water from salts. |
| **BPA** | Brine Processor Assembly | Processador de salmoura do MSFC que eleva a recuperação total de água do circuito fechado de 93,5% para 98%–99,4%. | MSFC Brine Processor Assembly boosting closed-loop water recovery from 93.5% up to 98%–99.4%. |
| **AWP** | Advanced Water Processor / Algal Wastewater Processor | Biorreator de tratamento de águas residuais que acopla filtração biológica ao cultivo de microalgas. | Wastewater bioreactor coupling biological filtration with microalgae cultivation. |
| **ARS** | Air Revitalization System | Subsistema integrado de revitalização do ar responsável por gerar $\text{O}_2$ e remover $\text{CO}_2$ e traços contaminantes. | Integrated Air Revitalization System responsible for generating $\text{O}_2$ and scrubbing $\text{CO}_2$ and trace contaminants. |
| **OGA** | Oxygen Generation Assembly | Gerador de oxigênio por eletrólise da água (2,3 a 9,2 kg $\text{O}_2$/dia), produzindo $\text{H}_2$ para o reator Sabatier. | Oxygen Generation Assembly using water electrolysis (2.3–9.2 kg $\text{O}_2$/day), supplying $\text{H}_2$ to the Sabatier reactor. |
| **PEM** | Proton Exchange Membrane | Membrana de troca de prótons usada nas células eletrolíticas do OGA para dividir $\text{H}_2\text{O}$ em $\text{O}_2$ e $\text{H}_2$. | Proton Exchange Membrane used in OGA electrolysis cells to split $\text{H}_2\text{O}$ into $\text{O}_2$ and $\text{H}_2$. |
| **CDRA** | Carbon Dioxide Removal Assembly | Leito adsorvente de 4 estágios (peneira molecular) que captura $\text{CO}_2$ exalado pela tripulação. | Four-bed molecular sieve adsorber that captures $\text{CO}_2$ exhaled by the crew. |
| **SABATIER / CRA** | Sabatier Carbon Dioxide Reduction Assembly | Reator químico ($\text{CO}_2 + 4\text{H}_2 \rightarrow \text{CH}_4 + 2\text{H}_2\text{O}$) que regenera água e produz bio-metano propelente. | Chemical reactor ($\text{CO}_2 + 4\text{H}_2 \rightarrow \text{CH}_4 + 2\text{H}_2\text{O}$) regenerating water and producing bio-methane propellant. |
| **BOSCH / MPA** | Bosch Reactor / Methane Pyrolysis Assembly | Sistema do MSFC que craqueia $\text{CH}_4$ em carbono sólido e $\text{H}_2$ para recuperar ainda mais água e oxigênio do $\text{CO}_2$. | MSFC system cracking $\text{CH}_4$ into solid carbon and $\text{H}_2$ to recover additional water and oxygen from $\text{CO}_2$. |
| **TCCS** | Trace Contaminant Control System | Sistema catalítico com íons de prata que elimina gases tóxicos, amônia e biofilmes do ar da cabine. | Silver-ion catalytic system removing toxic trace gases, ammonia, and biofilms from cabin air. |
| **EATCS** | External Active Thermal Control System | Sistema de controle térmico ativo externo (radiadores de amônia) que rejeita calor excessivo ($\text{kW}_t$) no vácuo. | External Active Thermal Control System (ammonia radiators) rejecting excess heat ($\text{kW}_t$) into space. |
| **PBR** | Photobioreactor | Fotobiorreator iluminado por LEDs UV/PAR onde microalgas *Chlorella* consomem $\text{CO}_2$ e geram $\text{O}_2$ e biomassa. | Photobioreactor illuminated by UV/PAR LEDs where *Chlorella* microalgae consume $\text{CO}_2$ and produce $\text{O}_2$ and biomass. |
| **APH** | Advanced Plant Habitat | Câmara botânica automatizada (Veggie/APH) que suplementa a dieta da tripulação e amplia a regeneração de $\text{O}_2$. | Automated botanical chamber (Veggie/APH) supplementing crew nutrition and expanding $\text{O}_2$ regeneration. |
| **HPGT** | High-Pressure Gas Tank | Tanque de gás de alta pressão na ISS usado para armazenar $\text{O}_2$ e $\text{N}_2$ separados por membrana. | High-Pressure Gas Tank on the ISS used to store membrane-separated $\text{O}_2$ and $\text{N}_2$. |
| **$\text{N}_2/\text{O}_2$** | Nitrogen / Oxygen Cabin Atmosphere | Mistura gasosa respirável da cabine mantida por separadores de membrana para compensar perdas em eclusas. | Breathable cabin gas mixture maintained by membrane separators to offset airlock losses. |

---

## Crew Biology, Radiation & Countermeasures / Biologia da Tripulação, Radiação e Contramedidas

| Sigla / Acronym | Nome Completo / Full Name | Descrição (pt-BR) | Description (en-US) |
| :--- | :--- | :--- | :--- |
| **EVA** | Extravehicular Activity | Atividade extraveicular (caminhada espacial ou exploração em superfície lunar/marciana) que desgasta os trajes. | Extravehicular Activity (spacewalk or lunar/Martian surface sortie) driving suit and hardware wear. |
| **xEMU** | Exploration Extravehicular Mobility Unit | Traje espacial de nova geração para Lua e Marte, modelado quanto à durabilidade (%) e resistência à poeira. | Next-generation spacesuit for Moon and Mars, modeled for durability (%) and dust resistance. |
| **PLSS** | Portable Life Support System | Mochila portátil de suporte à vida integrada ao traje xEMU para oxigênio, resfriamento e remoção de $\text{CO}_2$. | Portable Life Support System backpack integrated into the xEMU suit for oxygen, cooling, and $\text{CO}_2$ scrubbing. |
| **Z-2** | NASA Z-2 Prototype Planetary Suit | Protótipo de traje planetário com entrada traseira (*SuitPort*) projetado para reduzir entrada de poeira na cabine. | Planetary spacesuit prototype with rear-entry *SuitPort* designed to minimize cabin dust intrusion. |
| **SANS** | Spaceflight Associated Neuro-ocular Syndrome | Síndrome neuro-ocular associada ao voo espacial em microgravidade, mitigada por gravidade artificial e LBNP. | Spaceflight Associated Neuro-ocular Syndrome in microgravity, mitigated by artificial gravity and LBNP. |
| **ARED** | Advanced Resistive Exercise Device | Equipamento de exercício resistivo avançado usado na ISS para combater perda de massa óssea e muscular. | Advanced Resistive Exercise Device used on the ISS to counter bone density loss and muscle atrophy. |
| **LBNP** | Lower Body Negative Pressure | Câmara de pressão negativa para membros inferiores que redistribui fluidos corporais e reduz o risco de SANS. | Lower Body Negative Pressure chamber redistributing body fluids to reduce SANS risk. |
| **GCR** | Galactic Cosmic Rays | Raios Cósmicos Galácticos altamente energéticos no espaço profundo (Lua e Marte), atenuados por blindagem BNNT. | High-energy Galactic Cosmic Rays in deep space (Moon and Mars), attenuated by BNNT shielding. |
| **SAA** | South Atlantic Anomaly | Anomalia Magnética do Atlântico Sul onde a ISS atravessa níveis elevados de radiação em LEO. | South Atlantic Anomaly where the ISS passes through elevated trapped-radiation levels in LEO. |
| **ATOX** | Atomic Oxygen | Oxigênio atômico corrosivo presente na termosfera (LEO/ISS) que degrada polímeros e superfícies externas. | Corrosive atomic oxygen in the thermosphere (LEO/ISS) that degrades external polymers and coatings. |
| **BNNT** | Boron Nitride Nanotubes | Nanotubos de nitreto de boro ricos em hidrogênio/boro usados na blindagem leve contra radiação ($\text{g/cm}^2$). | Boron Nitride Nanotubes used in lightweight radiation shielding ($\text{g/cm}^2$). |
| **UV** | Ultraviolet Radiation | Radiação ultravioleta solar intensa em ambientes sem atmosfera protetora (ou atenuada em Marte). | Intense solar ultraviolet radiation in unshielded space environments (or partially attenuated on Mars). |

---

## Propulsion, Power & Ship Engineering / Propulsão, Energia e Engenharia da Nave

| Sigla / Acronym | Nome Completo / Full Name | Descrição (pt-BR) | Description (en-US) |
| :--- | :--- | :--- | :--- |
| **$\text{CH}_4/\text{LOX}$** | Liquid Methane / Liquid Oxygen | Propelente criogênico (Methalox) sintetizado pelo acoplamento Sabatier + biorreator para a queima de retorno. | Cryogenic propellant (Methalox) synthesized by the Sabatier + bioreactor loop for the return burn. |
| **MOXIE** | Mars Oxygen ISRU Experiment | Experimento da NASA (Perseverance) que produz $\text{O}_2$ a partir do $\text{CO}_2$ atmosférico de Marte. | NASA experiment (Perseverance) producing $\text{O}_2$ from Martian atmospheric $\text{CO}_2$. |
| **SOE** | Solid Oxide Electrolysis | Eletrólise em óxido sólido (base do MOXIE) para converter $\text{CO}_2$ marciano em $\text{O}_2$ e combustível. | Solid Oxide Electrolysis (core of MOXIE) converting Martian $\text{CO}_2$ into $\text{O}_2$ and fuel. |
| **KRUSTY** | Kilopower Reactor Using Stirling Technology | Reator nuclear de fissão compacto da NASA/LANL que fornece energia elétrica contínua independente da luz solar. | Compact NASA/LANL fission reactor providing continuous electrical power independent of sunlight. |
| **SABERS** | Solid-State Architecture Batteries for Enhanced Rechargeability and Safety | Arquitetura de baterias de estado sólido (Li-S) da NASA Glenn para armazenamento de alta densidade ($\text{kWh}$). | NASA Glenn solid-state battery architecture (Li-S) for high-density energy storage ($\text{kWh}$). |
| **EDS** | Electrodynamic Dust Shield | Escudo eletrodinâmico que repele poeira abrasiva lunar ou marciana de trajes, painéis e juntas mecânicas. | Electrodynamic Dust Shield repelling abrasive lunar or Martian dust from suits, panels, and seals. |
| **CNC** | Computer Numerical Control | Manufatura subtrativa/aditiva por controle numérico computadorizado para imprimir peças de reposição a bordo. | Computer Numerical Control manufacturing used to fabricate spare parts and tools aboard the ship. |
| **$\text{Li-S}$** | Lithium-Sulfur Battery | Química de bateria de lítio-enxofre leve adotada nos bancos de energia da nave-bioma. | Lightweight lithium-sulfur battery chemistry used in the Bio-Ship's energy storage banks. |
| **$\text{kWe / kWt / kWh}$** | Kilowatt-electric / Kilowatt-thermal / Kilowatt-hour | Unidades de potência elétrica gerada, capacidade de rejeição térmica por radiadores e energia armazenada. | Units of generated electrical power, radiator thermal rejection capacity, and stored energy. |
| **$\text{g/cm}^2$** | Grams per Square Centimeter | Densidade superficial da blindagem contra radiação (BNNT/água/polietileno) ao redor do módulo habitável. | Areal density of radiation shielding (BNNT/water/polyethylene) surrounding the habitable module. |
| **Gbps** | Gigabits per Second | Taxa de largura de banda óptica (DSOC) usada para telemedicina e redução do desgaste operacional. | Optical communication bandwidth rate (DSOC) supporting telemedicine and reduced operational wear. |

---

## Software, ML.NET & Simulator UI / Software, ML.NET e Interface do Simulador

| Sigla / Acronym | Nome Completo / Full Name | Descrição (pt-BR) | Description (en-US) |
| :--- | :--- | :--- | :--- |
| **ML.NET** | Machine Learning for .NET | Framework de aprendizado de máquina usado para treinar o Quad-Ensemble `FastTree` em `x64`. | Machine learning framework used to train the `FastTree` Quad-Ensemble on `x64`. |
| **WASM** | WebAssembly | Formato binário de navegador onde roda o frontend **Simulator** (Blazor WebAssembly) com inferência C# pura. | Browser binary runtime hosting the **Simulator** Blazor WebAssembly frontend with pure managed C# inference. |
| **TUI** | Text-based User Interface | Interface interativa em terminal do console app construída com `Spectre.Console` e barras ANSI finas. | Interactive terminal user interface in the console app built with `Spectre.Console` and thin ANSI bars. |
| **UI** | User Interface | Interface gráfica web construída com componentes nativos do `MudBlazor` (temas claro/escuro e `MudTimeline`). | Web graphical interface built with native `MudBlazor` components (light/dark themes and `MudTimeline`). |
| **ANSI** | American National Standards Institute (Escape Codes) | Códigos de escape de cores e blocos finos (`━` / `─`) usados nas barras de telemetria do terminal. | Color and thin-bar escape codes (`━` / `─`) used for terminal telemetry gauges. |
| **CLI** | Command-Line Interface | Interface de linha de comando (`--target`, `--lang`, `--export-only`, `--output`) do `SimulatorML`. | Command-line interface (`--target`, `--lang`, `--export-only`, `--output`) of `SimulatorML`. |
| **CI/CD** | Continuous Integration / Continuous Deployment | Pipeline automatizado (GitHub Actions) que treina os modelos, exporta o JSON e publica no GitHub Pages. | Automated pipeline (GitHub Actions) that trains the models, exports JSON artifacts, and deploys to GitHub Pages. |
| **JSON** | JavaScript Object Notation | Formato portátil usado em `bioship_fasttree_ensemble.json`, `bioship_decks_catalog.json` e `bioship_nasa_snapshot.json`. | Portable data format used in `bioship_fasttree_ensemble.json`, `bioship_decks_catalog.json`, and `bioship_nasa_snapshot.json`. |
| **CSV** | Comma-Separated Values | Formato tabular usado para armazenar as 240.000 amostras de treinamento (`bioship_3deck_240k_telemetry.csv`). | Tabular format storing the 240,000 training samples (`bioship_3deck_240k_telemetry.csv`). |
| **D1 / D2 / D3** | Deck 1 / Deck 2 / Deck 3 | Abreviações dos três baralhos combinados: `D1` (Ship Build), `D2` (Marshall ECLSS) e `D3` (Environment). | Shorthand for the three combined card decks: `D1` (Ship Build), `D2` (Marshall ECLSS), and `D3` (Environment). |
| **BLD / ECL / ENV / SYN** | Build / ECLSS / Environment / Synergy IDs | Prefixos dos códigos de cartas e combos no catálogo JSON (ex.: `BLD-01`, `ECL-01`, `ENV-MAR-01`, `SYN-*`). | Card and combo ID prefixes in the JSON catalog (e.g., `BLD-01`, `ECL-01`, `ENV-MAR-01`, `SYN-*`). |
