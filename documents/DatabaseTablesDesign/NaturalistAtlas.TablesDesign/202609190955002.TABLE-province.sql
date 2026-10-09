DROP TABLE IF EXISTS province;
CREATE TABLE province (
    id SERIAL PRIMARY KEY,
    country_id INT NOT NULL,
    code VARCHAR(10),
    name VARCHAR(100) NOT NULL,
    name_en VARCHAR(100) NOT NULL,
    lng NUMERIC(9,6) NOT NULL,
    lat NUMERIC(9,6) NOT NULL,
    status SMALLINT NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE province IS '省级行政区(国家一级区划,泛称省份:省/州/都道府县/大区等)';
COMMENT ON COLUMN province.id IS '主键';
COMMENT ON COLUMN province.country_id IS '国家id(country.id,无外键,业务层校验)';
COMMENT ON COLUMN province.code IS 'ISO 3166-2 一级区划代码(如CN-BJ/US-NY,可空)';
COMMENT ON COLUMN province.name IS '区划名称(中文)';
COMMENT ON COLUMN province.name_en IS '区划名称(英文)';
COMMENT ON COLUMN province.lng IS '地图定位中心点经度(前端地点搜索定位用)';
COMMENT ON COLUMN province.lat IS '地图定位中心点纬度(前端地点搜索定位用)';
COMMENT ON COLUMN province.status IS '状态(1=启用,0=停用)';
COMMENT ON COLUMN province.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN province.update_time IS '修改时间(UTC)';

CREATE INDEX idx_province_country ON province (country_id);
CREATE UNIQUE INDEX uq_province_country_code ON province (country_id, code);
