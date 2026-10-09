-- =====================================================================
-- 基础数据（城市管理 + 分类单元管理） 后台菜单种子（增量脚本，id 段 100-109）
-- 说明：
--   1) 基础数据分组(id=100)挂两组页面菜单：城市（列表/添加/编辑）、分类单元（列表/详情）
--   2) 分类单元由同步任务维护，后台只读（无添加/编辑/写操作菜单）
--   3) action_id 与 PermissionLoader 反射扫描出的 [PermissionAction] 全名对齐
--   4) 权限文案已预置于 _localization_resources/ManagementPermission/{default,zh-CN,en-US}.json
--      （CityManagement / CityOperation / TaxonManagement 分组）
--   5) 写操作接口（保存/启停/删除城市）不带菜单，仅由 Resource 权限控制
--   6) 新菜单需管理员在「后台菜单管理」页面手工添加对应权限分组才会对普通管理员可见（超管默认可见全部）
-- 幂等：按固定 id 写入前先清理，可重复执行
-- =====================================================================

-- 先清理旧记录（重复执行安全）
DELETE FROM manage_menu WHERE id BETWEEN 100 AND 109;

-- ---------------------------------------------------------------------
-- 基础数据（分组）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(100, '基础数据', 'basis-data', '', 0, '', '', 20, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ---------------------------------------------------------------------
-- 城市管理（100 段：101-103）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(101, '城市管理', 'city-list', '', 100, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.CityController.City_List', '/City/City_List', 100, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
-- 添加/编辑共用编辑页，各挂一个权限以便菜单层级展示（通常从列表行内进入）
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(102, '添加城市', 'city-add', '', 101, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.CityController.City_Add', '/City/City_Add', 90, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(103, '编辑城市', 'city-edit', '', 101, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.CityController.City_Edit', '/City/City_Edit', 85, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ---------------------------------------------------------------------
-- 分类单元管理（100 段：104-105）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(104, '分类单元', 'taxon-list', '', 100, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.TaxonController.Taxon_List', '/Taxon/Taxon_List', 90, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(105, '分类单元详情', 'taxon-detail', '', 104, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.TaxonController.Taxon_Detail', '/Taxon/Taxon_Detail', 85, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

SELECT setval(pg_get_serial_sequence('manage_menu', 'id'), GREATEST((SELECT MAX(id) FROM manage_menu), 109));
