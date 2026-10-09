DROP TABLE IF EXISTS taxon;
CREATE TABLE taxon (
    id VARCHAR(100) PRIMARY KEY,
    source SMALLINT NOT NULL,
    source_taxon_id VARCHAR(100) NOT NULL,
    rank SMALLINT NOT NULL,
    scientific_name VARCHAR(200) NOT NULL,
    common_name_cn VARCHAR(200) NOT NULL DEFAULT '',
    common_name_en VARCHAR(200) NOT NULL DEFAULT '',
    iconic_taxon SMALLINT NOT NULL,
    ancestry VARCHAR(500) NOT NULL DEFAULT '',
    threatened BOOLEAN NOT NULL DEFAULT FALSE,
    status SMALLINT NOT NULL DEFAULT 0,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE taxon IS '分类单元';
COMMENT ON COLUMN taxon.id IS '主键(雪花Id)';
COMMENT ON COLUMN taxon.source IS '数据源(1=iNaturalist,2=GBIF)';
COMMENT ON COLUMN taxon.source_taxon_id IS '源站分类ID(原样字符串入库:数字/UUID/QID/标本号等,仅等值匹配不排序)';
COMMENT ON COLUMN taxon.rank IS '分类阶元(0=未知,1=界,2=门,3=纲,4=目,5=科,6=属,7=种,8=亚种,9=变种,10=变型,11=杂交)';
COMMENT ON COLUMN taxon.scientific_name IS '学名';
COMMENT ON COLUMN taxon.common_name_cn IS '中文常用名';
COMMENT ON COLUMN taxon.common_name_en IS '英文常用名';
COMMENT ON COLUMN taxon.iconic_taxon IS '类群(1=鸟,2=哺乳,3=两栖,4=爬行动物,5=辐鳍鱼,6=昆虫,7=蛛形,8=软体,9=植物,10=真菌,11=其他动物,12=原生动物,13=色藻,14=未知,与iNat 13个iconic类群一一映射)';
COMMENT ON COLUMN taxon.ancestry IS '祖先链(源站分类ID串,如48460-1-2-3)';
COMMENT ON COLUMN taxon.threatened IS '受威胁物种(源站红色名录等保守状态标记,前端照实标注)';
COMMENT ON COLUMN taxon.status IS '状态(1=正常,0=源站已弃用)';
COMMENT ON COLUMN taxon.create_time IS '创建时间(UTC)';
COMMENT ON COLUMN taxon.update_time IS '修改时间(UTC)';

CREATE UNIQUE INDEX uq_taxon_source ON taxon (source, source_taxon_id);
CREATE INDEX idx_taxon_iconic ON taxon (iconic_taxon);
CREATE INDEX IF NOT EXISTS idx_taxon_source_ancestry ON taxon (source, ancestry);
CREATE INDEX IF NOT EXISTS idx_taxon_source_update_time ON taxon (source, update_time);
