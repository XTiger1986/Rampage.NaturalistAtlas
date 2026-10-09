-- =====================================================================
-- 内容治理（观察数据 + 举报工单） 后台菜单种子（增量脚本，id 段 110-119）
-- 说明：
--   1) 内容治理分组(id=110)挂两组页面菜单：观察数据（列表/详情）、举报工单（列表/处理）
--   2) 观察数据由采集端同步维护，后台只读 + 隐藏/恢复/按作者隐藏（写操作走 Resource 权限，不设菜单）
--   3) action_id 与 PermissionLoader 反射扫描出的 [PermissionAction] 全名对齐
--   4) 权限文案预置于 _localization_resources/ManagementPermission/{default,zh-CN,en-US}.json
--      （ObservationManagement / ObservationOperation / ReportManagement / ReportOperation 分组）
-- 幂等：按固定 id 写入前先清理，可重复执行
-- =====================================================================

-- 先清理旧记录（重复执行安全）
DELETE FROM manage_menu WHERE id BETWEEN 110 AND 119;

-- ---------------------------------------------------------------------
-- 内容治理（分组）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(110, '内容治理', 'content-moderation', '', 0, '', '', 30, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ---------------------------------------------------------------------
-- 观察数据（110 段：111-112）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(111, '观察数据', 'observation-list', '', 110, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.ObservationController.Observation_List', '/Observation/Observation_List', 100, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(112, '观察数据详情', 'observation-detail', '', 111, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.ObservationController.Observation_Detail', '/Observation/Observation_Detail', 90, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ---------------------------------------------------------------------
-- 举报工单（110 段：113-114）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(113, '举报工单', 'report-list', '', 110, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.ReportController.Report_List', '/Report/Report_List', 90, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(114, '处理举报工单', 'report-handle', '', 113, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.ReportController.Report_Handle', '/Report/Report_Handle', 85, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

SELECT setval(pg_get_serial_sequence('manage_menu', 'id'), GREATEST((SELECT MAX(id) FROM manage_menu), 114));
