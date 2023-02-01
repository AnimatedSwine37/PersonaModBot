module default {
    type ForumConfig {
        required property forumId -> int64 {
            constraint exclusive;
        }
        link helperConfig -> ForumHelperConfig;
        link tipConfig -> ForumTipConfig;
    }

    type ForumHelperConfig {
        required property solvedTag -> int64;
        required property solvedMessage -> str;
        required multi link allowedRoles -> RoleConfig {
            constraint exclusive;
        }
    }

    type ForumTipConfig {
        required property postTip -> bool;
        required property tipMessage -> str;
    }

    type RoleConfig {
        required property roleId -> int64;
        required property allowSolve -> bool;
        required property allowTag -> bool;
        required property allowRename -> bool;
    }

    type GuildConfig {
        required property guildId -> int64 {
            constraint exclusive;
        }
        required multi link forumConfigs -> ForumConfig {
            constraint exclusive;
        }
    }
}
