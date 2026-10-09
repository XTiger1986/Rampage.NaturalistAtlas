DROP TABLE IF EXISTS report;
CREATE TABLE report (
    id VARCHAR(100) PRIMARY KEY,
    observation_id VARCHAR(100) NOT NULL,
    reason SMALLINT NOT NULL,
    content VARCHAR(500) NOT NULL DEFAULT '',
    contact VARCHAR(100) NOT NULL DEFAULT '',
    status SMALLINT NOT NULL DEFAULT 0,
    handle_admin_id INTEGER NOT NULL DEFAULT 0,
    handle_time TIMESTAMP NULL,
    handle_remark VARCHAR(200) NOT NULL DEFAULT '',
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE report IS '内容举报';
COMMENT ON COLUMN report.id IS '主键(雪花Id)';
COMMENT ON COLUMN report.observation_id IS 'observation.id(雪花Id)';
COMMENT ON COLUMN report.reason IS '举报原因(0=其他,1=不当内容,2=错误鉴定,3=隐私问题,4=垃圾信息)';
COMMENT ON COLUMN report.content IS '补充说明';
COMMENT ON COLUMN report.contact IS '联系方式(可选,匿名举报可留空)';
COMMENT ON COLUMN report.status IS '状态(0=待处理,1=已隐藏,2=驳回)';
COMMENT ON COLUMN report.handle_admin_id IS '处理人admin.id(管理后台库)';
COMMENT ON COLUMN report.handle_time IS '处理时间(UTC,可空:未处理)';
COMMENT ON COLUMN report.handle_remark IS '处理备注';
COMMENT ON COLUMN report.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN report.update_time IS '修改时间(UTC)';

CREATE INDEX idx_report_status ON report (status, create_time);
CREATE INDEX idx_report_observation ON report (observation_id);
