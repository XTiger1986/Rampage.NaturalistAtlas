DROP TABLE IF EXISTS admin_operation_log;
CREATE TABLE admin_operation_log (
    id BIGSERIAL PRIMARY KEY,
    admin_id INTEGER NOT NULL,
    operation_name VARCHAR(200) NOT NULL DEFAULT '',
    area_name VARCHAR(100) NOT NULL,
    controller_name VARCHAR(100) NOT NULL,
    action_name VARCHAR(100) NOT NULL,
    raw_url VARCHAR(1000) NOT NULL,
    data_type_name VARCHAR(200) NOT NULL DEFAULT '',
    data_in TEXT NOT NULL,
    data_out TEXT NOT NULL,
    post_data TEXT NOT NULL,
    cookie_data TEXT NOT NULL,
    headers TEXT NOT NULL,
    referrer VARCHAR(1000) NOT NULL DEFAULT '',
    operation_ip VARCHAR(30) NOT NULL DEFAULT '',
    user_agent VARCHAR(255) NOT NULL DEFAULT '',
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE admin_operation_log IS '管理员操作记录';
COMMENT ON COLUMN admin_operation_log.admin_id IS '管理员ID';
COMMENT ON COLUMN admin_operation_log.operation_name IS '操作名称';
COMMENT ON COLUMN admin_operation_log.area_name IS '区域名';
COMMENT ON COLUMN admin_operation_log.controller_name IS '控制器名';
COMMENT ON COLUMN admin_operation_log.action_name IS '视图名';
COMMENT ON COLUMN admin_operation_log.raw_url IS '原始请求地址';
COMMENT ON COLUMN admin_operation_log.data_type_name IS '操作数据类型完整名称';
COMMENT ON COLUMN admin_operation_log.data_in IS '入口数据值';
COMMENT ON COLUMN admin_operation_log.data_out IS '出口数据值';
COMMENT ON COLUMN admin_operation_log.post_data IS '表单数据';
COMMENT ON COLUMN admin_operation_log.cookie_data IS 'COOKIE数据';
COMMENT ON COLUMN admin_operation_log.headers IS '头信息';
COMMENT ON COLUMN admin_operation_log.referrer IS '引用路径';
COMMENT ON COLUMN admin_operation_log.operation_ip IS '操作者IP';
COMMENT ON COLUMN admin_operation_log.user_agent IS '操作者客户端';
COMMENT ON COLUMN admin_operation_log.create_time IS '创建时间';
COMMENT ON COLUMN admin_operation_log.update_time IS '修改时间';

CREATE INDEX idx_admin_operation_log_admin_id ON admin_operation_log (admin_id);
CREATE INDEX idx_admin_operation_log_mvc ON admin_operation_log (controller_name, action_name, area_name);