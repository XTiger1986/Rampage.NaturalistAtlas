-- =====================================================================
-- 地理档案（国家 + 省份） 后台菜单种子（增量脚本，id 段 150-156）
-- 说明：
--   1) 挂「基础数据」分组(id=100)下：国家（列表/添加/编辑）、省份（列表/添加/编辑）
--   2) 城市编辑页的国家→省份级联下拉走 api/province/list（Query 权限，不带菜单）
--   3) action_id 与 PermissionLoader 反射扫描出的 [PermissionAction] 全名对齐
--   4) 权限文案预置于 _localization_resources/ManagementPermission/{default,zh-CN,en-US}.json
--      （CountryManagement / CountryOperation / ProvinceManagement / ProvinceOperation 分组）
-- 幂等：按固定 id 写入前先清理，可重复执行
-- =====================================================================

-- 先清理旧记录（重复执行安全）
DELETE FROM manage_menu WHERE id BETWEEN 150 AND 156;

-- ---------------------------------------------------------------------
-- 国家（150 段：151-153）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(151, '国家管理', 'country-list', '', 100, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.CountryController.Country_List', '/Country/Country_List', 80, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(152, '添加国家', 'country-add', '', 151, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.CountryController.Country_Add', '/Country/Country_Add', 75, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(153, '编辑国家', 'country-edit', '', 151, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.CountryController.Country_Edit', '/Country/Country_Edit', 74, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ---------------------------------------------------------------------
-- 省份（150 段：154-156）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(154, '省份管理', 'province-list', '', 100, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.ProvinceController.Province_List', '/Province/Province_List', 70, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(155, '添加省份', 'province-add', '', 154, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.ProvinceController.Province_Add', '/Province/Province_Add', 65, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(156, '编辑省份', 'province-edit', '', 154, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.ProvinceController.Province_Edit', '/Province/Province_Edit', 64, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

SELECT setval(pg_get_serial_sequence('manage_menu', 'id'), GREATEST((SELECT MAX(id) FROM manage_menu), 156));
