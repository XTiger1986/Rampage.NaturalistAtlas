DROP TABLE IF EXISTS emergence_history;
CREATE TABLE emergence_history (
    id VARCHAR(100) PRIMARY KEY,
    city_id INTEGER NOT NULL,
    taxon_id VARCHAR(100) NOT NULL,
    month SMALLINT NOT NULL,
    record_count INTEGER NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE emergence_history IS '物候全历史缓存(源站直方图,R1.2全历史口径用)';
COMMENT ON COLUMN emergence_history.id IS '主键(雪花Id)';
COMMENT ON COLUMN emergence_history.city_id IS 'city.id';
COMMENT ON COLUMN emergence_history.taxon_id IS 'taxon.id(雪花Id)';
COMMENT ON COLUMN emergence_history.month IS '月份(1-12)';
COMMENT ON COLUMN emergence_history.record_count IS '全历史出现次数(源站直方图口径)';
COMMENT ON COLUMN emergence_history.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN emergence_history.update_time IS '修改时间(UTC)';

CREATE UNIQUE INDEX uq_emergence_history_month ON emergence_history (city_id, taxon_id, month);
