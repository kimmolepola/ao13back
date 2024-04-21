import knexlib from "knex";
import * as authActions from "./user.actions";
import * as passwordResetTokenActions from "./passwordResetToken.actions";
import * as periodicTasks from "./periodicTasks";

const config = {
  client: "sqlite3",
  connection: {
    filename: "./local/asdf.db",
  },
};

export const knex = knexlib(config);

export const initialize = () => {
  authActions.initialize();
  passwordResetTokenActions.initialize();
  periodicTasks.initialize();
};
