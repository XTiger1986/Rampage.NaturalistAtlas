-- =====================================================================
-- 采集同步（任务状态 + 同步日志） 后台菜单种子（增量脚本，id 段 140-149）
-- 说明：
--   1) 采集同步分组(id=140)挂两组页面菜单：任务（列表/参数编辑）、日志（列表/详情）
--   2) 手动触发按钮为预留禁用位，待采集端（Collector）触发机制接入后开放（无独立 action）
--   3) action_id 与 PermissionLoader 反射扫描出的 [PermissionAction] 全名对齐
--   4) 权限文案预置于 _localization_resources/ManagementPermission/{default,zh-CN,en-US}.json
--      （SyncManagement / SyncOperation 分组）
-- 幂等：按固定 id 写入前先清理，可重复执行
-- =====================================================================

-- 先清理旧记录（重复执行安全）
DELETE FROM manage_menu WHERE id BETWEEN 140 AND 149;

-- ---------------------------------------------------------------------
-- 采集同步（分组）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(140, '采集同步', 'sync-mgm', '', 0, '', '', 40, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ---------------------------------------------------------------------
-- 采集任务（140 段：141-142）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(141, '采集任务', 'sync-task-list', '', 140, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.SyncController.Task_List', '/Sync/Task_List', 100, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(142, '采集任务配置', 'sync-task-edit', '', 141, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.SyncController.Task_Edit', '/Sync/Task_Edit', 90, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ---------------------------------------------------------------------
-- 同步日志（140 段：143-144）
-- ---------------------------------------------------------------------
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(143, '同步日志', 'sync-log-list', '', 140, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.SyncController.Log_List', '/Sync/Log_List', 90, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(144, '同步日志详情', 'sync-log-detail', '', 143, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.SyncController.Log_Detail', '/Sync/Log_Detail', 85, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

SELECT setval(pg_get_serial_sequence('manage_menu', 'id'), GREATEST((SELECT MAX(id) FROM manage_menu), 144));
