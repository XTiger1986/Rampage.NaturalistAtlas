DROP TABLE IF EXISTS admin_in_group;
CREATE TABLE admin_in_group (
    id SERIAL PRIMARY KEY,
    admin_id INTEGER NOT NULL,
    group_id INTEGER NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE admin_in_group IS '管理员所在组';
COMMENT ON COLUMN admin_in_group.admin_id IS '管理员ID';
COMMENT ON COLUMN admin_in_group.group_id IS '管理组';
COMMENT ON COLUMN admin_in_group.status IS '状态';
COMMENT ON COLUMN admin_in_group.create_time IS '创建时间';
COMMENT ON COLUMN admin_in_group.update_time IS '修改时间';