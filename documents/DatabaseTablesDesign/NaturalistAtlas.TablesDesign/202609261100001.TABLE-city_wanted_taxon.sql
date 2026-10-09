DROP TABLE IF EXISTS city_wanted_taxon;
CREATE TABLE city_wanted_taxon (
    id VARCHAR(100) PRIMARY KEY,
    city_id INTEGER NOT NULL,
    source SMALLINT NOT NULL DEFAULT 1,
    source_taxon_id VARCHAR(100) NOT NULL,
    rank SMALLINT NOT NULL DEFAULT 0,
    scientific_name VARCHAR(200) NOT NULL DEFAULT '',
    common_name_cn VARCHAR(200) NOT NULL DEFAULT '',
    common_name_en VARCHAR(200) NOT NULL DEFAULT '',
    iconic_taxon SMALLINT NOT NULL DEFAULT 14,
    nearby_count INTEGER NOT NULL DEFAULT 0,
    photo_square_url VARCHAR(500) NOT NULL DEFAULT '',
    photo_medium_url VARCHAR(500) NOT NULL DEFAULT '',
    photo_license SMALLINT NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE city_wanted_taxon IS '城市集邮缺口缓存（wanted 分类快照：周边已记录、本市未记录；采集端每周全量重写，公开接口只读）';
COMMENT ON COLUMN city_wanted_taxon.id IS '主键(雪花Id)';
COMMENT ON COLUMN city_wanted_taxon.city_id IS 'city.id';
COMMENT ON COLUMN city_wanted_taxon.source IS '数据源(1=iNaturalist,2=GBIF)';
COMMENT ON COLUMN city_wanted_taxon.source_taxon_id IS '源站分类id(wanted 分类本市无记录，本地 taxon 表大概率缺失，故冗余存名称与图片)';
COMMENT ON COLUMN city_wanted_taxon.rank IS '分类阶元(0=未知,1=界...11=杂交,同taxon.rank)';
COMMENT ON COLUMN city_wanted_taxon.scientific_name IS '学名';
COMMENT ON COLUMN city_wanted_taxon.common_name_cn IS '中文常用名';
COMMENT ON COLUMN city_wanted_taxon.common_name_en IS '英文常用名';
COMMENT ON COLUMN city_wanted_taxon.iconic_taxon IS '类群代码(1~14,同observation.iconic_taxon)';
COMMENT ON COLUMN city_wanted_taxon.nearby_count IS '周边记录次数(源站口径)';
COMMENT ON COLUMN city_wanted_taxon.photo_square_url IS '代表图方图URL';
COMMENT ON COLUMN city_wanted_taxon.photo_medium_url IS '代表图中图URL';
COMMENT ON COLUMN city_wanted_taxon.photo_license IS '代表图授权协议(0=保留所有权利,1~8同observation.license,wanted端点default_photo.license_code映射)';
COMMENT ON COLUMN city_wanted_taxon.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN city_wanted_taxon.update_time IS '修改时间(UTC,每城全量重写时更新)';

CREATE UNIQUE INDEX uq_city_wanted_taxon ON city_wanted_taxon (city_id, source, source_taxon_id);
CREATE INDEX idx_city_wanted_iconic ON city_wanted_taxon (city_id, iconic_taxon);

-- 说明：wanted_sync 任务的 sync_cursor 行（source=1, city_id=0）由采集端 runner 首次运行时自动创建，
-- config.CityIndex 记录城市轮转断点，BatchSize 为单城 wanted 条数上限。
