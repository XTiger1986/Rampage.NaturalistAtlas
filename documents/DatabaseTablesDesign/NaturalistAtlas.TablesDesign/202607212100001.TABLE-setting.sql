DROP TABLE IF EXISTS setting CASCADE;
CREATE TABLE setting (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    type VARCHAR(255) NOT NULL,
    data JSONB NOT NULL CHECK (jsonb_typeof(data) = 'object'),
    create_admin_id int NOT NULL,
	update_admin_id int NOT NULL DEFAULT 0,
    create_time TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMPTZ NULL
);

COMMENT ON TABLE setting IS '系统设置';
COMMENT ON COLUMN setting.name IS '名称';
COMMENT ON COLUMN setting.type IS '对应的类型完整名称';
COMMENT ON COLUMN setting.data IS '设置JSON(在程序代码中定义各类DTO模型)';
COMMENT ON COLUMN setting.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN setting.update_time IS '修改时间(UTC)';

CREATE UNIQUE INDEX uq_setting_name ON setting (name);

SELECT setval(pg_get_serial_sequence('setting', 'id'), 9999);

