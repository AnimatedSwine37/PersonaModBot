CREATE MIGRATION m1whms4etlxhsfb2qdljaebmj476h22iszmboxadtlvpmkc2xwn6pa
    ONTO m1kystsilqhyeofjvdymqsyaqggsyspyexpkugbov545xpenl35iwa
{
  ALTER TYPE default::ForumConfig {
      DROP LINK allowedRoles;
  };
  CREATE TYPE default::ForumHelperConfig {
      CREATE REQUIRED MULTI LINK allowedRoles -> default::RoleConfig {
          CREATE CONSTRAINT std::exclusive;
      };
      CREATE REQUIRED PROPERTY solvedMessage -> std::str;
      CREATE REQUIRED PROPERTY solvedTag -> std::int64;
  };
  ALTER TYPE default::ForumConfig {
      CREATE LINK helperConfig -> default::ForumHelperConfig;
  };
  CREATE TYPE default::ForumTipConfig {
      CREATE REQUIRED PROPERTY postTip -> std::bool;
      CREATE PROPERTY tipMessage -> std::str;
  };
  ALTER TYPE default::ForumConfig {
      CREATE LINK tipConfig -> default::ForumTipConfig;
  };
  ALTER TYPE default::ForumConfig {
      DROP PROPERTY postTip;
  };
  ALTER TYPE default::ForumConfig {
      DROP PROPERTY solvedMessage;
  };
  ALTER TYPE default::ForumConfig {
      DROP PROPERTY solvedTag;
  };
  ALTER TYPE default::ForumConfig {
      DROP PROPERTY tipMessage;
  };
};
