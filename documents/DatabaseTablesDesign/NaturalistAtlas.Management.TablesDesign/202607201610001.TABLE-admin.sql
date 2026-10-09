DROP TABLE IF EXISTS admin;
CREATE TABLE admin (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    password VARCHAR(200) NOT NULL,
    true_name VARCHAR(50) NOT NULL DEFAULT '',
    mobile VARCHAR(30) NOT NULL DEFAULT '',
    email VARCHAR(200) NOT NULL DEFAULT '',
    avatar VARCHAR(200) NOT NULL DEFAULT '',
    gender INTEGER NOT NULL DEFAULT 0,
    status INTEGER NOT NULL DEFAULT 0,
    is_master BOOLEAN NOT NULL DEFAULT FALSE,
    login_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00',
    login_ip VARCHAR(30) NOT NULL DEFAULT '',
    create_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_time TIMESTAMP NOT NULL DEFAULT '0001-01-01 00:00:00'
);
COMMENT ON TABLE admin IS '管理员';
COMMENT ON COLUMN admin.username IS '用户名';
COMMENT ON COLUMN admin.password IS '密码';
COMMENT ON COLUMN admin.true_name IS '真实姓名';
COMMENT ON COLUMN admin.mobile IS '手机';
COMMENT ON COLUMN admin.email IS '邮箱';
COMMENT ON COLUMN admin.avatar IS '头像';
COMMENT ON COLUMN admin.gender IS '性别';
COMMENT ON COLUMN admin.status IS '状态';
COMMENT ON COLUMN admin.is_master IS '超级管理员';
COMMENT ON COLUMN admin.login_time IS '最后登录时间';
COMMENT ON COLUMN admin.login_ip IS '最后登录IP';
COMMENT ON COLUMN admin.create_time IS '创建时间';
COMMENT ON COLUMN admin.update_time IS '修改时间';

-- 种子管理员原始密码：R0ZCbNoNJbgtFk3K（PBKDF2-SHA256 哈希存储，登录后请修改密码并清理本注释）
INSERT INTO admin (id, username, password, true_name, mobile, email, status, is_master)
VALUES (1, 'master', 'PBKDF2-SHA256$100000$6efGN5MUV5f4LmpKbr1mEQ==$4UyV/zK8BGQYK0Hl1cDUm1SW2Td/G0W/Fk48EtkEPpI=', '管理员', '', '', 1, TRUE);
SELECT setval(pg_get_serial_sequence('admin', 'id'), 1);