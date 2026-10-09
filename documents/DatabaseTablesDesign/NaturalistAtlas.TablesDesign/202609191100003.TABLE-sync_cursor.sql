DROP TABLE IF EXISTS sync_cursor;
CREATE TABLE sync_cursor (
    id SERIAL PRIMARY KEY,
    source SMALLINT NOT NULL,
    city_id INTEGER NOT NULL,
    task VARCHAR(50) NOT NULL,
    watermark TIMESTAMP NULL,
    config JSONB NOT NULL DEFAULT '{}'::jsonb CHECK (jsonb_typeof(config) = 'object'),
    last_run_status SMALLINT NOT NULL DEFAULT 0,
    last_run_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00',
    last_run_message VARCHAR(500) NOT NULL DEFAULT '',
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE sync_cursor IS '采集水位';
COMMENT ON COLUMN sync_cursor.id IS '主键';
COMMENT ON COLUMN sync_cursor.source IS '数据源(1=iNaturalist,2=GBIF)';
COMMENT ON COLUMN sync_cursor.city_id IS 'city.id';
COMMENT ON COLUMN sync_cursor.task IS '任务类型(increment=增量,backfill=回补,taxon_refresh=分类刷新,deletion_check=删除对账,histogram=直方图)';
COMMENT ON COLUMN sync_cursor.watermark IS '增量水位(源站updated_at,UTC,可空:尚未开始采集)';
COMMENT ON COLUMN sync_cursor.config IS '任务参数与进度(回补时间窗切片等,程序内定义DTO)';
COMMENT ON COLUMN sync_cursor.last_run_status IS '上次运行状态(0=未运行,1=成功,2=失败,3=熔断跳过)';
COMMENT ON COLUMN sync_cursor.last_run_time IS '上次运行时间(UTC)';
COMMENT ON COLUMN sync_cursor.last_run_message IS '上次运行摘要';
COMMENT ON COLUMN sync_cursor.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN sync_cursor.update_time IS '修改时间(UTC)';

CREATE UNIQUE INDEX uq_sync_cursor_task ON sync_cursor (source, city_id, task);
