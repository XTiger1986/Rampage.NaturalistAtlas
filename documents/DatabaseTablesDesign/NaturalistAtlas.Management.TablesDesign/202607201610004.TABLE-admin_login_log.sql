DROP TABLE IF EXISTS admin_login_log;
CREATE TABLE admin_login_log (
    id BIGSERIAL PRIMARY KEY,
    admin_id INTEGER NOT NULL,
    login_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    login_ip VARCHAR(30) NOT NULL DEFAULT '',
    user_agent VARCHAR(255) NOT NULL DEFAULT ''
);
COMMENT ON TABLE admin_login_log IS '管理员登录记录';
COMMENT ON COLUMN admin_login_log.admin_id IS '管理员id';
COMMENT ON COLUMN admin_login_log.login_time IS '登录时间';
COMMENT ON COLUMN admin_login_log.login_ip IS '登录IP';
COMMENT ON COLUMN admin_login_log.user_agent IS '登录客户端';

CREATE INDEX idx_admin_id ON admin_login_log (admin_id);