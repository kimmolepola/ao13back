import { knex } from "./db";
import * as types from "../types";

export const initialize = async () => {
  try {
    console.log("--init table users");
    const result = await knex.schema.hasTable("passwordResetTokens");
    console.log("--init table users result:", result);
    if (!result) {
      console.log("--create");
      await knex.schema.createTable("passwordResetTokens", (table) => {
        table.string("userId").primary().notNullable();
        table.string("token").notNullable();
        table.integer("createdAt").notNullable();
      });
    }
  } catch (err) {
    console.log("passwordResetTokens initiation error:", err);
  }
};

export const findByUserId = async (userId: string) => {
  try {
    const result: types.PasswordResetToken[] = await knex(
      "passwordResetTokens"
    ).where({
      userId,
    });
    return result[0];
  } catch (err: any) {
    console.error("passwordResetTokens findByUserId error:", err);
  }
};

export const deleteToken = async (token: string) => {
  try {
    const numberOrAffectedRows: number = await knex("passwordResetTokens")
      .where({
        token,
      })
      .del();
    return numberOrAffectedRows;
  } catch (err: any) {
    console.error("passwordResetTokens deleteToken error:", err);
  }
};

export const insertToken = async (
  passwordResetToken: types.PasswordResetToken
) => {
  try {
    await knex("passwordResetTokens").insert(passwordResetToken);
  } catch (err: any) {
    console.error("passwordResetTokens insertToken error:", err);
  }
};

export const deleteExpiredTokens = async (expirationTime: number) => {
  try {
    const numberOrAffectedRows: number = await knex("passwordResetTokens")
      .where("createdAt", "<", expirationTime)
      .del();
    return numberOrAffectedRows;
  } catch (err: any) {
    console.error("passwordResetTokens deleteExpiredTokens error:", err);
  }
};
