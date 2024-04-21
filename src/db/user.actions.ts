import { knex } from "./db";
import * as types from "../types";

export const initialize = async () => {
  try {
    const result = await knex.schema.hasTable("users");
    if (!result) {
      await knex.schema.createTable("users", (table) => {
        table.string("id").primary().notNullable();
        table.string("email").notNullable();
        table.string("username", 50).notNullable();
        table.string("password").notNullable();
        table.integer("score");
      });
    }
  } catch (err) {
    console.log("users initiation error:", err);
  }
};

export const insertUser = (user: types.User) => knex("users").insert(user);

export const findByEmail = async (email: string) => {
  try {
    const result: types.User[] = await knex("users").where({ email });
    return result[0];
  } catch (err: any) {
    console.error("users findByEmail error:", err);
  }
};

export const findByUsername = async (username: string) => {
  try {
    const result: types.User[] = await knex("users").where({ username });
    return result[0];
  } catch (err: any) {
    console.error("users findByUsername error:", err);
  }
};

export const findById = async (id: string) => {
  try {
    const result: types.User[] = await knex("users").where({ id });
    return result[0];
  } catch (err: any) {
    console.error("users findById error:", err);
  }
};

export const updatePassword = async (id: string, password: string) => {
  try {
    await knex("users").where({ id }).update({ password });
  } catch (err: any) {
    console.error("users updatePassword error:", err);
  }
};

export const udpateUsername = async (id: string, username: string) => {
  try {
    await knex("users").where({ id }).update({ username });
  } catch (err: any) {
    console.error("users updateUsername error:", err);
  }
};

export const updateScore = async (id: string, score: number) => {
  try {
    await knex("users").where({ id }).update({ score });
  } catch (err: any) {
    console.error("users updateScore error:", err);
  }
};
