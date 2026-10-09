DROP TABLE IF EXISTS observation;
CREATE TABLE observation (
    id VARCHAR(100) PRIMARY KEY,
    source SMALLINT NOT NULL,
    source_observation_id VARCHAR(100) NOT NULL,
    city_id INTEGER NOT NULL,
    taxon_id VARCHAR(100) NOT NULL,
    iconic_taxon SMALLINT NOT NULL,
    quality_grade SMALLINT NOT NULL,
    observed_on DATE NOT NULL,
    observed_at TIMESTAMP NULL,
    lng NUMERIC(9,6) NOT NULL,
    lat NUMERIC(9,6) NOT NULL,
    positional_accuracy INTEGER NULL,
    geoprivacy SMALLINT NOT NULL DEFAULT 0,
    photo_square_url VARCHAR(500) NOT NULL DEFAULT '',
    photo_medium_url VARCHAR(500) NOT NULL DEFAULT '',
    photo_large_url VARCHAR(500) NOT NULL DEFAULT '',
    license SMALLINT NOT NULL DEFAULT 0,
    user_login VARCHAR(255) NOT NULL DEFAULT '',
    record_basis SMALLINT NULL,
    dataset_key VARCHAR(36) NULL,
    dataset_title VARCHAR(200) NULL,
    identifications_count INTEGER NOT NULL DEFAULT 0,
    source_created_at TIMESTAMP NULL,
    source_updated_at TIMESTAMP NULL,
    status SMALLINT NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE observation IS '观察记录主档表';
COMMENT ON COLUMN observation.id IS '主键(雪花Id)';
COMMENT ON COLUMN observation.source IS '数据源(1=iNaturalist,2=GBIF)';
COMMENT ON COLUMN observation.source_observation_id IS '源站观察ID(原样字符串入库:数字/UUID/URN等,仅等值匹配不排序)';
COMMENT ON COLUMN observation.city_id IS 'city.id';
COMMENT ON COLUMN observation.taxon_id IS 'taxon.id(雪花Id)';
COMMENT ON COLUMN observation.iconic_taxon IS '类群(冗余自taxon,取值同taxon.iconic_taxon)';
COMMENT ON COLUMN observation.quality_grade IS '鉴定级别(1=research,2=needs_id,3=casual预留)';
COMMENT ON COLUMN observation.observed_on IS '观察日期(观察者本地日期,物候聚合口径)';
COMMENT ON COLUMN observation.observed_at IS '观察时刻(UTC,可空:部分记录仅有日期)';
COMMENT ON COLUMN observation.lng IS '公开坐标经度';
COMMENT ON COLUMN observation.lat IS '公开坐标纬度';
COMMENT ON COLUMN observation.positional_accuracy IS '位置精度(米,源站值,可空)';
COMMENT ON COLUMN observation.geoprivacy IS '坐标保护(0=开放,1=模糊,2=私密)';
COMMENT ON COLUMN observation.photo_square_url IS '照片方图URL(75,卡片缩略)';
COMMENT ON COLUMN observation.photo_medium_url IS '照片中图URL(500,卡片)';
COMMENT ON COLUMN observation.photo_large_url IS '照片大图URL(1024,详情)';
COMMENT ON COLUMN observation.license IS '首图授权(0=保留所有权利,1=CC0,2=CC-BY,3=CC-BY-SA,4=CC-BY-NC,5=CC-BY-NC-SA,6=CC-BY-ND,7=CC-BY-NC-ND,8=PDM公有领域标记)';
COMMENT ON COLUMN observation.user_login IS '观察者公开昵称(GBIF为recordedBy,可含多人)';
COMMENT ON COLUMN observation.record_basis IS '记录类型(0=人类观察,1=机器观察,2=标本;可空:iNat不分机器/标本时填人类观察)';
COMMENT ON COLUMN observation.dataset_key IS 'GBIF数据集UUID(数据发布机构标识,iNat来源留空;兼作跳过iNat转发记录的对账锚点)';
COMMENT ON COLUMN observation.dataset_title IS 'GBIF数据集名称(卡片署名数据发布机构用,iNat来源留空)';
COMMENT ON COLUMN observation.identifications_count IS '鉴定次数';
COMMENT ON COLUMN observation.source_created_at IS '源站发布时间(UTC,新鲜度度量用)';
COMMENT ON COLUMN observation.source_updated_at IS '源站最后更新时间(UTC,增量水位与对账用)';
COMMENT ON COLUMN observation.status IS '状态(0=正常,1=隐藏(运营处置),2=源站已删除(对账下线))';
COMMENT ON COLUMN observation.create_time IS '创建时间(本站入库时间,UTC)';
COMMENT ON COLUMN observation.update_time IS '修改时间(UTC)';

CREATE UNIQUE INDEX uq_observation_source ON observation (source, source_observation_id);
CREATE INDEX idx_observation_city_observed ON observation (city_id, observed_on);
CREATE INDEX idx_observation_taxon_observed ON observation (taxon_id, observed_on);
CREATE INDEX idx_observation_city_iconic ON observation (city_id, iconic_taxon);
CREATE INDEX idx_observation_city_grade ON observation (city_id, quality_grade);
CREATE INDEX IF NOT EXISTS idx_observation_source_city_status ON observation (source, city_id, status);
