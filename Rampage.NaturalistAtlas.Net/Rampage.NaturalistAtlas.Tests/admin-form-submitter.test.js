/**
 * 管理后台修改密码 / 资料保存：FormSubmitter 的非 2xx 分类，以及页面仍按 [FromBody] 提交。
 * 运行：node --test Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.Tests/admin-form-submitter.test.js
 */
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const test = require('node:test');

const repoRoot = path.resolve(__dirname, '..', '..');
const rpgPath = path.join(
    repoRoot,
    'Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.ManagementSite/wwwroot/assets/js/rpg-core.js'
);
const passwordView = path.join(
    repoRoot,
    'Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.ManagementSite/Views/Personal/ChangePassword.cshtml'
);
const profileView = path.join(
    repoRoot,
    'Rampage.NaturalistAtlas.Net/Rampage.NaturalistAtlas.ManagementSite/Views/Personal/EditProfile.cshtml'
);

function loadRpg() {
    const sandbox = {
        console,
        jQuery: function () {
            return {};
        }
    };
    sandbox.window = sandbox;
    vm.runInNewContext(fs.readFileSync(rpgPath, 'utf8'), sandbox, { filename: rpgPath });
    return sandbox.RPG;
}

const lengthFailure = {
    Code: 5100,
    Message: 'Fail to verify data',
    IsSuccess: false,
    ValidationResult: {
        Success: false,
        Details: {
            Password: { Name: 'Password', Description: '新密码长度必须在5~20位之间' },
            VerifyPassword: { Name: 'VerifyPassword', Description: '原密码长度必须在5~20位之间' }
        }
    }
};

const bare415 = {
    type: 'https://tools.ietf.org/html/rfc9110#section-15.5.16',
    title: 'Unsupported Media Type',
    status: 415
};

test('R validation body is a business failure, not a network error', () => {
    const RPG = loadRpg();
    const classified = RPG.classifyAjaxFailure(lengthFailure, 400);
    assert.equal(classified.kind, 'business');
    assert.equal(classified.payload.ValidationResult.Details.Password.Description, '新密码长度必须在5~20位之间');
});

test('bare 415 problem+json stays a network failure', () => {
    const RPG = loadRpg();
    assert.equal(RPG.classifyAjaxFailure(bare415, 415).kind, 'network');
    assert.equal(RPG.classifyAjaxFailure(null, 415).kind, 'network');
});

test('HTML and empty bodies stay network failures', () => {
    const RPG = loadRpg();
    const html = RPG.readAjaxJson({ responseText: '<!DOCTYPE html><html><title>Login</title></html>' });
    assert.equal(html, null);
    assert.equal(RPG.classifyAjaxFailure(html, 200).kind, 'network');
    assert.equal(RPG.classifyAjaxFailure(null, 0).kind, 'network');
});

test('problem+json field errors are business failures', () => {
    const RPG = loadRpg();
    const payload = {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
            '$.Gender': ['The JSON value could not be converted to Rampage.Enums.UserGender.']
        }
    };
    assert.equal(RPG.classifyAjaxFailure(payload, 400).kind, 'business');
    const parsed = RPG.readAjaxJson({ responseText: JSON.stringify(payload) });
    assert.equal(parsed.errors['$.Gender'][0].includes('UserGender'), true);
});

test('change-password posts JSON and does not send PasswordConfirm', () => {
    const source = fs.readFileSync(passwordView, 'utf8');
    assert.match(source, /useBody:\s*true/);
    assert.match(source, /delete data\.PasswordConfirm/);
    assert.match(source, /name="VerifyPassword"/);
    assert.match(source, /name="Password"/);
    assert.match(source, /name="PasswordConfirm"/);
});

test('profile save posts JSON and sends Gender as a number', () => {
    const source = fs.readFileSync(profileView, 'utf8');
    assert.match(source, /useBody:\s*true/);
    assert.match(source, /data\.Gender = parseInt\(data\.Gender, 10\)/);
    assert.match(source, /verify-name="Gender"/);
});
