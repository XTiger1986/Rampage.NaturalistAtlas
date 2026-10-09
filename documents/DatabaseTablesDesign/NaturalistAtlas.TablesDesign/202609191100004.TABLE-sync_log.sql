DROP TABLE IF EXISTS sync_log;
CREATE TABLE sync_log (
    id VARCHAR(100) PRIMARY KEY,
    source SMALLINT NOT NULL,
    city_id INTEGER NOT NULL,
    task VARCHAR(50) NOT NULL,
    status SMALLINT NOT NULL DEFAULT 0,
    start_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP NULL,
    request_count INTEGER NOT NULL DEFAULT 0,
    insert_count INTEGER NOT NULL DEFAULT 0,
    update_count INTEGER NOT NULL DEFAULT 0,
    fail_count INTEGER NOT NULL DEFAULT 0,
    message VARCHAR(1000) NOT NULL DEFAULT '',
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE sync_log IS '采集运行日志';
COMMENT ON COLUMN sync_log.id IS '主键(雪花Id)';
COMMENT ON COLUMN sync_log.source IS '数据源(1=iNaturalist,2=GBIF)';
COMMENT ON COLUMN sync_log.city_id IS 'city.id';
COMMENT ON COLUMN sync_log.task IS '任务类型(取值同sync_cursor.task)';
COMMENT ON COLUMN sync_log.status IS '状态(0=运行中,1=成功,2=失败,3=熔断跳过)';
COMMENT ON COLUMN sync_log.start_time IS '开始时间(UTC)';
COMMENT ON COLUMN sync_log.end_time IS '结束时间(UTC,可空:运行中)';
COMMENT ON COLUMN sync_log.request_count IS '请求次数';
COMMENT ON COLUMN sync_log.insert_count IS '新增条数';
COMMENT ON COLUMN sync_log.update_count IS '更新条数';
COMMENT ON COLUMN sync_log.fail_count IS '失败条数';
COMMENT ON COLUMN sync_log.message IS '运行摘要(失败原因等)';
COMMENT ON COLUMN sync_log.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN sync_log.update_time IS '修改时间(UTC)';

CREATE INDEX idx_sync_log_city_time ON sync_log (city_id, create_time);
