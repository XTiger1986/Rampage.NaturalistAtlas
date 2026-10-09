DROP TABLE IF EXISTS manage_group_permission;
CREATE TABLE manage_group_permission (
    id SERIAL PRIMARY KEY,
    group_id INTEGER NOT NULL,
    resource VARCHAR(200) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE manage_group_permission IS '管理组授权';
COMMENT ON COLUMN manage_group_permission.group_id IS '管理组ID';
COMMENT ON COLUMN manage_group_permission.resource IS '资源标识';
COMMENT ON COLUMN manage_group_permission.status IS '状态';
COMMENT ON COLUMN manage_group_permission.create_time IS '创建时间';
COMMENT ON COLUMN manage_group_permission.update_time IS '修改时间';