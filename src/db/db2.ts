import sqlite3 from "sqlite3";
import knexlib from "knex";
import * as authActions from "./user.actions";
import * as passwordResetTokenActions from "./passwordResetToken.actions";
import * as periodicTasks from "./periodicTasks";

let db: sqlite3.Database | undefined;

const config = {
  client: "sqlite3",
  connection: {
    filename: "./asdf.db",
  },
};

export const knex = knexlib(config);

export const initiate = () => {
  authActions.initiate();
  passwordResetTokenActions.initiate();
  periodicTasks.initiate();
};

export const connect = () => {
  // knex = knexlib(config);
  // sqlite3.verbose();
  // db = new sqlite3.Database("./db/asdf.db", (err) => {
  //   if (err) {
  //     console.error(err.message);
  //   }
  //   console.log("Connected to the asdf database.");
  // });
};

export const close = () => {
  db?.close((err) => {
    if (err) {
      console.error(err.message);
    }
    console.log("Close the database connection.");
  });
};

export const getKnex = () => {
  return knex;
};
