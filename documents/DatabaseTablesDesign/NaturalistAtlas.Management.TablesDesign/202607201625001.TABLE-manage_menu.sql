DROP TABLE IF EXISTS manage_menu;
CREATE TABLE manage_menu (
    id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL DEFAULT '',
    css VARCHAR(50) NOT NULL DEFAULT '',
    style VARCHAR(200) NOT NULL DEFAULT '',
    parent_id INTEGER NOT NULL DEFAULT 0,
    action_id VARCHAR(200) NOT NULL DEFAULT '0',
    url VARCHAR(200) NOT NULL DEFAULT '',
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_sys BOOLEAN NOT NULL DEFAULT FALSE,
    status INTEGER NOT NULL DEFAULT 1,
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE manage_menu IS '后台菜单';
COMMENT ON COLUMN manage_menu.title IS '标题';
COMMENT ON COLUMN manage_menu.css IS '自定义样式名';
COMMENT ON COLUMN manage_menu.style IS '自定义样式';
COMMENT ON COLUMN manage_menu.parent_id IS '上一级菜单';
COMMENT ON COLUMN manage_menu.action_id IS '对应功能ID';
COMMENT ON COLUMN manage_menu.url IS '地址';
COMMENT ON COLUMN manage_menu.sort_order IS '排序值 (值越大优先级越高)';
COMMENT ON COLUMN manage_menu.is_sys IS '是否系统内置';
COMMENT ON COLUMN manage_menu.status IS '状态';

INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,is_sys,create_time,update_time) VALUES(1, '后台权限管理', 'authority-mgm', '', 0, '', '', 0, 1, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,is_sys,create_time,update_time) VALUES(2, '管理组列表', 'group-list', '', 1, 'Rampage.Management.Mvc.AdminController.Group_List', '/Admin/Group_List', 80, 1, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,is_sys,create_time,update_time) VALUES(3, '添加管理组', 'group-add', '', 2, 'Rampage.Management.Mvc.AdminController.Group_Add', '/Admin/Group_Add', 1, 1, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,is_sys,create_time,update_time) VALUES(4, '管理员列表', 'admin-list', '', 1, 'Rampage.Management.Mvc.AdminController.Admin_List', '/Admin/Admin_List', 100, 1, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,is_sys,create_time,update_time) VALUES(5, '添加管理员', 'admin-add', '', 4, 'Rampage.Management.Mvc.AdminController.Admin_Add', '/Admin/Admin_Add', 1, 1, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,is_sys,create_time,update_time) VALUES(6, '后台菜单管理', 'manage-menu-list', '', 1, 'Rampage.Management.Mvc.AdminController.Menu_List', '/Admin/Menu_List', 20, 1, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,is_sys,create_time,update_time) VALUES(7, '添加后台菜单', 'manage-menu-add', '', 6, 'Rampage.Management.Mvc.AdminController.Menu_Add', '/Admin/Menu_Add', 1, 1, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,is_sys,create_time,update_time) VALUES(8, '管理日志列表', 'admin-operation-log', '', 1, 'Rampage.Management.Mvc.AdminController.AdminOperationLog_List', '/Admin/AdminOperationLog_List', 10, 1, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(90, '系统设置', 'sys-config', '', 0, '', '', 10, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
INSERT INTO manage_menu(id,title,css,style,parent_id,action_id,url,sort_order,status,create_time,update_time) VALUES(91, '常规设置', 'general-config', '', 90, 'Rampage.NaturalistAtlas.ManagementSite.Controllers.SettingController.GeneralConfig', '/Setting/GeneralConfig', 100, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

SELECT setval(pg_get_serial_sequence('manage_menu', 'id'), 99);
