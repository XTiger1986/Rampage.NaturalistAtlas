DROP TABLE IF EXISTS country;
CREATE TABLE country (
    id SERIAL PRIMARY KEY,
    code CHAR(2) NOT NULL,
    name VARCHAR(100) NOT NULL,
    name_en VARCHAR(100) NOT NULL,
    lng NUMERIC(9,6) NOT NULL,
    lat NUMERIC(9,6) NOT NULL,
    status SMALLINT NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE country IS '国家';
COMMENT ON COLUMN country.id IS '主键';
COMMENT ON COLUMN country.code IS 'ISO 3166-1 alpha-2 国家代码(小写,如cn/gb)';
COMMENT ON COLUMN country.name IS '国家名称(中文)';
COMMENT ON COLUMN country.name_en IS '国家名称(英文)';
COMMENT ON COLUMN country.lng IS '地图定位中心点经度(前端地点搜索定位用)';
COMMENT ON COLUMN country.lat IS '地图定位中心点纬度(前端地点搜索定位用)';
COMMENT ON COLUMN country.status IS '状态(1=启用,0=停用)';
COMMENT ON COLUMN country.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN country.update_time IS '修改时间(UTC)';

CREATE UNIQUE INDEX uq_country_code ON country (code);
