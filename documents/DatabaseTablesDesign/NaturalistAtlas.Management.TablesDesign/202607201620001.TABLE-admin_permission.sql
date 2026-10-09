DROP TABLE IF EXISTS admin_permission;
CREATE TABLE admin_permission (
    id SERIAL PRIMARY KEY,
    admin_id INTEGER NOT NULL,
    resource VARCHAR(200) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE admin_permission IS '管理员授权';
COMMENT ON COLUMN admin_permission.admin_id IS '管理员ID';
COMMENT ON COLUMN admin_permission.resource IS '资源标识';
COMMENT ON COLUMN admin_permission.status IS '状态';
COMMENT ON COLUMN admin_permission.create_time IS '创建时间';
COMMENT ON COLUMN admin_permission.update_time IS '修改时间';