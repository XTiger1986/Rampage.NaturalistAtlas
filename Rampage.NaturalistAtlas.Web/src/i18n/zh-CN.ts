/**
 * 中文语言包（界面中文优先，所有 key 以此文件为准，en-US 须保持同结构）
 */
export default {
  app: {
    name: '自然地图志',
    nameEn: 'Naturalist Atlas',
    slogan: '在地图上重新看见身边的自然'
  },
  nav: {
    explore: '探索',
    overview: '总览',
    species: '物种榜',
    phenology: '物候',
    gaps: '集邮缺口'
  },
  common: {
    loading: '加载中…',
    empty: '暂无数据',
    retry: '重试',
    all: '全部',
    records: '条记录',
    observers: '位观察者',
    submit: '提交',
    cancel: '取消',
    viewDetail: '查看详情',
    collapse: '收起',
    expand: '展开',
    networkError: '网络不可用或服务未启动',
    requestFailed: '请求失败',
    backendHint: '后端服务未连接；地图与底图仍可用，数据稍后会自动恢复'
  },
  format: {
    justNow: '刚刚',
    unidentified: '未鉴定',
    allRightsReserved: '保留所有权利'
  },
  img: {
    loadedKb: '已加载 {kb} KB',
    failed: '图片加载失败',
    retry: '点击重试'
  },
  city: {
    select: '选择城市',
    offline: '离线城市列表（后端未连接）'
  },
  search: {
    placeholder: '搜索国家 / 省份 / 城市',
    country: '国家',
    province: '省份',
    city: '城市',
    recent: '最近搜索',
    noResult: '暂未收录，欢迎反馈'
  },
  theme: {
    title: '视觉主题',
    journal: '博物志·纸本',
    journalDesc: '暖纸、宋体、标本签',
    night: '夜观·萤火',
    nightDesc: '深色沉浸、发光类群',
    spectrum: '四季·光谱',
    spectrumDesc: '明亮通透、色随月份'
  },
  map: {
    mode2d: '2D',
    mode25d: '2.5D',
    mode3d: '3D',
    mode2dTip: '平面地图',
    mode25dTip: '倾斜视角 + 建筑',
    mode3dTip: '地形 + 地球仪',
    layers: '图层',
    basemap: '底图',
    overlays: '叠加图层',
    cards: '照片卡',
    heatmap: '热力图',
    global: '全球视角',
    cityBounds: '城市范围',
    globalProvider: '全球数据源',
    truncated: '当前条件下共 {total} 条，已显示最新 {n} 条，放大或加筛选查看更多',
    globalHint: '已切换到全球视角（数据源官方密度瓦片）',
    globalBar: '全球视角 · 数据来自 iNaturalist / GBIF 官方瓦片 · 点击回到 {city}',
    globalDegraded: '全球瓦片暂时不可用（源站无响应），已暂时隐藏全球图层',
    obscured: '位置经保护性模糊'
  },
  basemap: {
    ofm: 'OpenFreeMap 矢量',
    tianditu: '天地图',
    imagery: '卫星影像'
  },
  filter: {
    quality: '鉴定状态',
    research: '研究级',
    needsId: '待鉴定',
    clear: '清除筛选',
    play: '播放「城市苏醒」',
    pause: '暂停',
    monthUnit: '月',
    unit_month: '月',
    unit_tenDay: '旬',
    present: '投屏模式',
    presentExit: '退出投屏'
  },
  season: {
    script: {
      1: '冬鸟在结冰的湖面聚群，常绿植物撑起一月的绿意',
      2: '二月柳芽未发，听一听越冬水鸟与渐起的早春鸟鸣',
      3: '山樱与早蝶登场，城市苏醒自树梢开始',
      4: '候鸟过境高峰，抬头看看天空的雁阵与柳絮',
      5: '五月花事最盛，传粉昆虫在花丛中忙碌',
      6: '萤火虫的季节，夜观湿地与草丛的微光',
      7: '盛夏蝉鸣鼎沸，蜻蜓与雨后蛙声占领城市',
      8: '八月暑气蒸腾，昆虫多样性达到全年峰值',
      9: '候鸟秋季迁徙开启，燕群开始集结南行',
      10: '真菌季到来，雨后的林地藏着伞下的世界',
      11: '十一月层林尽染，最后的秋叶与南下的冬鸭',
      12: '岁末观鸟正当时，越冬水鸟陆续抵达'
    }
  },
  taxon: {
    Birds: '鸟类',
    Mammals: '哺乳',
    Amphibians: '两栖',
    Reptiles: '爬行',
    RayFinnedFishes: '鱼类',
    Insects: '昆虫',
    Arachnids: '蛛形',
    Mollusks: '软体',
    Plants: '植物',
    Fungi: '真菌',
    OtherAnimals: '其他动物',
    Protozoans: '原生动物',
    Chromista: '色藻',
    Unknown: '未知'
  },
  explore: {
    latest: '最新记录',
    inView: '视野内 {n} 条',
    sortByDistance: '按距离排序'
  },
  overview: {
    title: '城市总览',
    total: '总记录',
    research: '研究级',
    speciesCount: '物种数',
    observerCount: '观察者',
    thisMonth: '本月记录',
    trend: '近 12 个月趋势',
    composition: '类群构成',
    risingStars: '本月新星',
    risingStarsTip: '本月首次累计达到 ≥5 条研究级记录的物种',
    reachedOn: '{date} 达标',
    topObservers: '活跃观察者',
    researchOnly: '仅研究级',
    freshness: '数据更新于 {t}'
  },
  species: {
    title: '城市物种榜',
    byRecord: '按记录数',
    byObserver: '按观察者数',
    byRare: '稀缺榜',
    rareTip: '稀缺榜口径：仅统计研究级 · 记录数升序（本城少见但已被证实的物种）',
    keyword: '搜索物种名',
    export: '导出 CSV',
    caliber: '统计口径：{city} · {quality} · {months}',
    bestWindow: '最佳观察时间：{from} ~ {to}（约 {pct}% 的记录）',
    photoWall: '照片墙',
    similar: '相似物种',
    rhythm: '全年节奏',
    firstSeen: '最早记录',
    lastSeen: '最近记录',
    threatened: '受威胁物种',
    csv: {
      scientificName: '学名',
      commonNameCn: '中文名',
      iconic: '类群',
      recordCount: '记录数',
      observerCount: '观察者数',
      lastObservedOn: '最近观察日期'
    }
  },
  phenology: {
    title: '物候日历',
    curve: '出现曲线',
    matrix: '物候矩阵',
    detailMode: '近两年明细（逐旬）',
    historyMode: '全历史统计（逐月）',
    allYear: '全年',
    speciesCol: '物种',
    sortByMonth: '按该月记录数排序',
    sortPeak: '按峰值月排序（季节节奏排成对角线）',
    sortPeakShort: '峰值',
    exportCaliber: '口径：{quality} · 行内归一化着色 · 观察热度 ≠ 种群数量',
    sampleSize: '样本量 {n} 条',
    caliberNote: '口径说明：曲线反映的是"被观察记录"的热度，而非真实种群数量；样本量小时请谨慎解读。',
    pickSpecies: '从物种榜或矩阵中选择一个物种查看曲线'
  },
  tenDay: {
    monthLabel: '{mon}',
    early: '{mon}上旬',
    mid: '{mon}中旬',
    late: '{mon}下旬'
  },
  taxonRank: {
    unknown: '生命',
    kingdom: '界',
    phylum: '门',
    class: '纲',
    order: '目',
    family: '科',
    genus: '属',
    species: '种',
    subspecies: '亚种',
    variety: '变种',
    form: '变型',
    hybrid: '杂交'
  },
  gaps: {
    title: '集邮缺口',
    desc: '这些物种在周边已有记录、本城市还没有——按类群筛选与地图图例联动，找到你身边的"第一笔记录"。',
    emptyTitle: '暂无缺口数据',
    emptyDesc: '数据每周更新一次；也欢迎你成为本城第一位记录者。',
    nearby: '周边已记录 {n} 次',
    nearbyUnknown: '周边记录较少',
    record: '去记录',
    source: '缺口口径：iNaturalist wanted（周边已记录 − 本市未记录），由采集端每周刷新'
  },
  obs: {
    observer: '观察者',
    observedOn: '观察日期',
    license: '授权',
    source: '数据来源',
    viewSource: '在源站查看',
    identifications: '鉴定次数',
    report: '举报',
    reported: '已举报',
    locate: '在地图上定位',
    publisher: '数据发布机构'
  },
  report: {
    title: '举报此观察',
    reason: '原因',
    reasons: ['其他', '不当内容', '错误鉴定', '隐私问题', '垃圾信息'],
    content: '补充说明（选填）',
    contact: '联系方式（选填）',
    success: '已收到举报，运营将尽快处理',
    fail: '提交失败，请稍后重试'
  },
  empty: {
    sparseTitle: '这座城市的记录还很少',
    sparseDesc: '成为第一批观察者：在 iNaturalist 记录你身边的自然，数据会在 2 小时内出现在这里。',
    join: '去 iNaturalist 记录'
  },
  notFound: {
    title: '这片区域还没有被探索',
    back: '回到地图'
  }
}
