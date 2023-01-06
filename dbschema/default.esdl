module default {
    type ForumConfig {
        required property forumId -> int64;
        required property solvedTag -> int64;
        required property solvedMessage -> str;
        required multi property allowedRoles -> int64;
    }

    type GuildConfig {
        required property guildId -> int64;
        required multi link forumConfigs -> ForumConfig {
            constraint exclusive;
        }
    }
}
