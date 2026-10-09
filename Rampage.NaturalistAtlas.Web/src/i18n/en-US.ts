/**
 * 英文语言包（预留；结构必须与 zh-CN.ts 一致，由类型约束保证）
 */
import type zhCN from './zh-CN'

const enUS: typeof zhCN = {
  app: {
    name: 'Naturalist Atlas',
    nameEn: 'Naturalist Atlas',
    slogan: 'Rediscover the nature around you, on a map'
  },
  nav: {
    explore: 'Explore',
    overview: 'Overview',
    species: 'Species',
    phenology: 'Phenology',
    gaps: 'Gaps'
  },
  common: {
    loading: 'Loading…',
    empty: 'No data',
    retry: 'Retry',
    all: 'All',
    records: 'records',
    observers: 'observers',
    submit: 'Submit',
    cancel: 'Cancel',
    viewDetail: 'Details',
    collapse: 'Collapse',
    expand: 'Expand',
    networkError: 'Network unavailable or the server is not running',
    requestFailed: 'Request failed',
    backendHint: 'Backend not connected; the map and base layers still work — data will resume automatically'
  },
  format: {
    justNow: 'Just now',
    unidentified: 'Unidentified',
    allRightsReserved: 'All rights reserved'
  },
  img: {
    loadedKb: '{kb} KB loaded',
    failed: 'Failed to load image',
    retry: 'Tap to retry'
  },
  city: {
    select: 'Select city',
    offline: 'Offline city list (backend not connected)'
  },
  search: {
    placeholder: 'Search country / province / city',
    country: 'Country',
    province: 'Province',
    city: 'City',
    recent: 'Recent searches',
    noResult: 'Not covered yet — feedback welcome'
  },
  theme: {
    title: 'Theme',
    journal: 'Field Journal',
    journalDesc: 'Paper, serif, specimen labels',
    night: 'Night Safari',
    nightDesc: 'Immersive dark, glowing taxa',
    spectrum: 'Seasonal Spectrum',
    spectrumDesc: 'Bright, color follows the month'
  },
  map: {
    mode2d: '2D',
    mode25d: '2.5D',
    mode3d: '3D',
    mode2dTip: 'Flat map',
    mode25dTip: 'Tilted view + buildings',
    mode3dTip: 'Terrain + globe',
    layers: 'Layers',
    basemap: 'Basemap',
    overlays: 'Overlays',
    cards: 'Photo cards',
    heatmap: 'Heatmap',
    global: 'Global view',
    cityBounds: 'City bounds',
    globalProvider: 'Global source',
    truncated: '{total} records matched; showing the latest {n}. Zoom in or filter to see more',
    globalHint: 'Switched to global view (official density tiles)',
    globalBar: 'Global view · tiles from iNaturalist / GBIF · click to return to {city}',
    globalDegraded: 'Global tiles are temporarily unavailable (source not responding) and have been hidden',
    obscured: 'Location obscured for protection'
  },
  basemap: {
    ofm: 'OpenFreeMap vector',
    tianditu: 'Tianditu',
    imagery: 'Satellite'
  },
  filter: {
    quality: 'Quality',
    research: 'Research',
    needsId: 'Needs ID',
    clear: 'Clear',
    play: 'Play "City Awakening"',
    pause: 'Pause',
    monthUnit: 'M',
    unit_month: 'Month',
    unit_tenDay: '10-day',
    present: 'Presentation mode',
    presentExit: 'Exit presentation',
  },
  season: {
    script: {
      1: 'Winter birds gather on icy lakes; evergreens keep the city green',
      2: 'Buds still closed — listen for overwintering waterbirds and first songs',
      3: 'Mountain cherry and early butterflies: the city wakes in the treetops',
      4: 'Peak migration — look up for goose skeins and drifting catkins',
      5: 'Full bloom: pollinators busy across the flower beds',
      6: 'Firefly season — faint glows in wetlands and grass after dark',
      7: 'Midsummer cicadas roar; dragonflies and post-rain frogs take over',
      8: 'August heat: insect diversity peaks for the year',
      9: 'Autumn migration begins — swallows gathering to head south',
      10: 'Fungus season: after rain, a hidden world opens under the canopy',
      11: 'Last autumn leaves, arriving winter ducks',
      12: 'Year-end birding: wintering waterfowl keep arriving'
    }
  },
  taxon: {
    Birds: 'Birds',
    Mammals: 'Mammals',
    Amphibians: 'Amphibians',
    Reptiles: 'Reptiles',
    RayFinnedFishes: 'Fishes',
    Insects: 'Insects',
    Arachnids: 'Arachnids',
    Mollusks: 'Mollusks',
    Plants: 'Plants',
    Fungi: 'Fungi',
    OtherAnimals: 'Other animals',
    Protozoans: 'Protozoans',
    Chromista: 'Chromista',
    Unknown: 'Unknown'
  },
  explore: {
    latest: 'Latest records',
    inView: '{n} in view',
    sortByDistance: 'Sort by distance'
  },
  overview: {
    title: 'City overview',
    total: 'Records',
    research: 'Research',
    speciesCount: 'Species',
    observerCount: 'Observers',
    thisMonth: 'This month',
    trend: 'Last 12 months',
    composition: 'Composition',
    risingStars: 'Rising stars',
    risingStarsTip: 'Species reaching ≥5 research-grade records for the first time this month',
    reachedOn: 'Reached on {date}',
    topObservers: 'Top observers',
    researchOnly: 'Research only',
    freshness: 'Updated {t}'
  },
  species: {
    title: 'Species ranking',
    byRecord: 'By records',
    byObserver: 'By observers',
    byRare: 'Rarity',
    rareTip: 'Rarity scope: research grade only · records ascending (rare but verified species here)',
    keyword: 'Search species',
    export: 'Export CSV',
    caliber: 'Scope: {city} · {quality} · {months}',
    bestWindow: 'Best time to observe: {from} – {to} (≈{pct}% of records)',
    photoWall: 'Photos',
    similar: 'Similar species',
    rhythm: 'Yearly rhythm',
    firstSeen: 'First seen',
    lastSeen: 'Last seen',
    threatened: 'Threatened',
    csv: {
      scientificName: 'Scientific name',
      commonNameCn: 'Chinese name',
      iconic: 'Group',
      recordCount: 'Records',
      observerCount: 'Observers',
      lastObservedOn: 'Last observed'
    }
  },
  phenology: {
    title: 'Phenology',
    curve: 'Emergence curve',
    matrix: 'Matrix',
    detailMode: 'Last 2 years (10-day)',
    historyMode: 'All-time (monthly)',
    allYear: 'All year',
    speciesCol: 'Species',
    sortByMonth: 'Sort by this month',
    sortPeak: 'Sort by peak month (seasonal rhythm along the diagonal)',
    sortPeakShort: 'peak',
    exportCaliber: 'Scope: {quality} · row-normalized colors · observation effort ≠ population',
    sampleSize: 'n = {n}',
    caliberNote: 'Curves reflect observation effort, not population size; interpret small samples with care.',
    pickSpecies: 'Pick a species from the ranking or matrix'
  },
  tenDay: {
    monthLabel: '{mon}',
    early: 'Early {mon}',
    mid: 'Mid {mon}',
    late: 'Late {mon}'
  },
  taxonRank: {
    unknown: 'Life',
    kingdom: 'Kingdom',
    phylum: 'Phylum',
    class: 'Class',
    order: 'Order',
    family: 'Family',
    genus: 'Genus',
    species: 'Species',
    subspecies: 'Subspecies',
    variety: 'Variety',
    form: 'Form',
    hybrid: 'Hybrid'
  },
  gaps: {
    title: 'Collection gaps',
    desc: 'Recorded nearby but not yet in this city — filter with the map legend and claim the first local record.',
    emptyTitle: 'No gaps yet',
    emptyDesc: 'Data refreshes weekly; you can also be the first to record.',
    nearby: '{n} nearby records',
    nearbyUnknown: 'Few nearby records',
    record: 'Record',
    source: 'Scope: iNaturalist wanted (nearby minus local), refreshed weekly by the collector'
  },
  obs: {
    observer: 'Observer',
    observedOn: 'Observed on',
    license: 'License',
    source: 'Source',
    viewSource: 'View on source site',
    identifications: 'Identifications',
    report: 'Report',
    reported: 'Reported',
    locate: 'Locate on map',
    publisher: 'Publisher'
  },
  report: {
    title: 'Report this observation',
    reason: 'Reason',
    reasons: ['Other', 'Inappropriate', 'Misidentification', 'Privacy', 'Spam'],
    content: 'Details (optional)',
    contact: 'Contact (optional)',
    success: 'Report received, thank you',
    fail: 'Submission failed, please retry'
  },
  empty: {
    sparseTitle: 'Few records in this city yet',
    sparseDesc: 'Be one of the first: record nature on iNaturalist and it will show up here within 2 hours.',
    join: 'Record on iNaturalist'
  },
  notFound: {
    title: 'This area is unexplored',
    back: 'Back to map'
  }
}

export default enUS
