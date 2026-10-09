DROP TABLE IF EXISTS emergence_stat;
CREATE TABLE emergence_stat (
    id VARCHAR(100) PRIMARY KEY,
    city_id INTEGER NOT NULL,
    taxon_id VARCHAR(100) NOT NULL,
    stat_year SMALLINT NOT NULL,
    ten_day SMALLINT NOT NULL,
    record_count INTEGER NOT NULL DEFAULT 0,
    research_count INTEGER NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE emergence_stat IS '预聚合物候统计(预留,数据量上来后启用)';
COMMENT ON COLUMN emergence_stat.id IS '主键(雪花Id)';
COMMENT ON COLUMN emergence_stat.city_id IS 'city.id';
COMMENT ON COLUMN emergence_stat.taxon_id IS 'taxon.id(雪花Id)';
COMMENT ON COLUMN emergence_stat.stat_year IS '统计年份';
COMMENT ON COLUMN emergence_stat.ten_day IS '旬序(1-36,每月1-10日/11-20日/21日-月末)';
COMMENT ON COLUMN emergence_stat.record_count IS '出现次数(research+needs_id)';
COMMENT ON COLUMN emergence_stat.research_count IS '出现次数(仅research)';
COMMENT ON COLUMN emergence_stat.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN emergence_stat.update_time IS '修改时间(UTC)';

CREATE UNIQUE INDEX uq_emergence_stat_period ON emergence_stat (city_id, taxon_id, stat_year, ten_day);
