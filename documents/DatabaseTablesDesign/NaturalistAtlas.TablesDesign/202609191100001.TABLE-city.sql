DROP TABLE IF EXISTS city;
CREATE TABLE city (
    id SERIAL PRIMARY KEY,
    country_id INT NOT NULL,
    province_id INT,
    name VARCHAR(50) NOT NULL,
    name_en VARCHAR(100),
    slug VARCHAR(50) NOT NULL,
    sw_lng NUMERIC(9,6) NOT NULL,
    sw_lat NUMERIC(9,6) NOT NULL,
    ne_lng NUMERIC(9,6) NOT NULL,
    ne_lat NUMERIC(9,6) NOT NULL,
    inat_place_id INT,
    boundary_wkt TEXT,
    status SMALLINT NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE city IS '城市';
COMMENT ON COLUMN city.id IS '主键';
COMMENT ON COLUMN city.country_id IS '国家id(country.id,无外键,业务层校验)';
COMMENT ON COLUMN city.province_id IS '省级行政区id(province.id,可空:城市国家或数据缺失,无外键,业务层校验)';
COMMENT ON COLUMN city.name IS '城市名称(中文)';
COMMENT ON COLUMN city.name_en IS '城市名称(英文,可空)';
COMMENT ON COLUMN city.slug IS 'URL标识(全站唯一,重名城市以{slug}-{国家码}消歧义,如cambridge-gb)';
COMMENT ON COLUMN city.sw_lng IS 'bbox西南角经度';
COMMENT ON COLUMN city.sw_lat IS 'bbox西南角纬度';
COMMENT ON COLUMN city.ne_lng IS 'bbox东北角经度';
COMMENT ON COLUMN city.ne_lat IS 'bbox东北角纬度';
COMMENT ON COLUMN city.inat_place_id IS 'iNaturalist place_id(源站行政区划过滤用,可空:回退bbox)';
COMMENT ON COLUMN city.boundary_wkt IS 'GBIF geometry参数WKT边界(简化多边形,可空:回退bbox矩形)';
COMMENT ON COLUMN city.status IS '状态(1=启用,0=停用)';
COMMENT ON COLUMN city.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN city.update_time IS '修改时间(UTC)';

CREATE UNIQUE INDEX uq_city_slug ON city (slug);
CREATE INDEX idx_city_country ON city (country_id);
CREATE INDEX idx_city_province ON city (province_id);
