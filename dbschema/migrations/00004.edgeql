CREATE MIGRATION m1vogy2oipa35m6a2q6s6magtejkvf3sdy7qochxbi7v4m23a6obcq
    ONTO m1whms4etlxhsfb2qdljaebmj476h22iszmboxadtlvpmkc2xwn6pa
{
  ALTER TYPE default::ForumTipConfig {
      ALTER PROPERTY tipMessage {
          SET REQUIRED USING ('A tip');
      };
  };
};
