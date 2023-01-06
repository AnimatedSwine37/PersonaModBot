CREATE MIGRATION m1kzx5w4xvtqutgm5j4z7tvpy2df7nxyahntrspn7lqc5qj6dljn6q
    ONTO initial
{
  CREATE FUTURE nonrecursive_access_policies;
  CREATE TYPE default::ForumConfig {
      CREATE REQUIRED MULTI PROPERTY AllowedRoles -> std::int64;
      CREATE REQUIRED PROPERTY forumId -> std::int64;
      CREATE REQUIRED PROPERTY solvedMessage -> std::str;
      CREATE REQUIRED PROPERTY solvedTag -> std::int64;
  };
  CREATE TYPE default::GuildConfig {
      CREATE REQUIRED MULTI LINK forumConfigs -> default::ForumConfig {
          CREATE CONSTRAINT std::exclusive;
      };
      CREATE REQUIRED PROPERTY guildId -> std::int64;
  };
};
