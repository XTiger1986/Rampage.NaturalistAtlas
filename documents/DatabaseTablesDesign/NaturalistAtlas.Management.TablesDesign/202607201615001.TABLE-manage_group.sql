DROP TABLE IF EXISTS manage_group;
CREATE TABLE manage_group (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(500) NOT NULL DEFAULT '',
    status INTEGER NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE manage_group IS '管理组';
COMMENT ON COLUMN manage_group.name IS '名称';
COMMENT ON COLUMN manage_group.description IS '描述';
COMMENT ON COLUMN manage_group.status IS '状态';
COMMENT ON COLUMN manage_group.create_time IS '创建时间';
COMMENT ON COLUMN manage_group.update_time IS '修改时间';