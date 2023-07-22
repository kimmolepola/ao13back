import "dotenv/config";
import crypto from "crypto";
import JWT from "jsonwebtoken";
import bcrypt from "bcrypt";
import { v4 as uuidv4 } from "uuid";
import sendEmail from "../utils/email/sendEmail";
import { disconnect } from "../index";
import * as userActions from "../db/user.actions";
import * as passwordResetTokenActions from "../db/passwordResetToken.actions";
import * as types from "../types";
import * as signupRequests from "./auth.service.signupRequests";

const JWTSecret = process.env.JWT_SECRET || "";
const bcryptSalt = process.env.BCRYPT_SALT;
const client =
  process.env.NODE_ENV === "production"
    ? `https://${process.env.CLIENT_HOST}`
    : `http://${process.env.CLIENT_HOST}:${process.env.DEV_CLIENT_PORT}`;

export const decode = (token: any) => {
  const decodedToken: any = JWT.verify(token, JWTSecret);
  if (!token || !decodedToken.id) {
    const err: any = new Error("Invalid or missing token");
    err.statusCode = 401;
    throw err;
  }
  return decodedToken;
};

/* eslint-disable no-underscore-dangle, no-return-assign, no-param-reassign */
export const logout = async (token: any) => {
  const { id } = decode(token);
  disconnect(id);
  console.log(`logout ${id}`);
  return true;
};

export const getTurnCredentials = (token: any) => {
  console.log("--get TURN credentials", token);
  const { id } = decode(token);
  const secretKey = process.env.HMAC_SECRET;
  if (!secretKey) {
    const err: any = new Error("Server error");
    err.statusCode = 500;
    throw err;
  }

  // this credential will be valid for the next 60 seconds
  const unixTimeStamp = Math.floor(Date.now() / 1000) + 60;
  const username = [unixTimeStamp, id].join(":");
  const hmac = crypto.createHmac("sha1", secretKey);
  hmac.setEncoding("base64");
  hmac.write(username);
  hmac.end();
  const password = hmac.read();
  return {
    urls: `turns:${process.env.TURN_URL}`,
    username,
    credential: password,
  };
};

export const login = async (data: any) => {
  console.log("login attempt:", data.username);
  let user: types.User | undefined;
  if (data.username.includes("@")) {
    user = await userActions.findByEmail(data.username);
  } else {
    user = await userActions.findByUsername(data.username);
  }
  const passwordCorrect =
    user && user.password
      ? await bcrypt.compare(data.password, user.password)
      : false;

  console.log("--user:", user, "--passwordCorrect:", passwordCorrect);

  if (!user || !passwordCorrect) {
    const err: any = new Error("Invalid username, email or password");
    err.statusCode = 401;
    throw err;
  }

  const token = JWT.sign({ id: user.id }, JWTSecret);

  console.log("logged in:", user.id, user.username);

  return (data = {
    score: user.score,
    userId: user.id,
    username: user.username,
    token,
  });
};

export const requestSignup = async (data: any) => {
  console.log("signup", data.email);
  const item = await userActions.findByEmail(data.email);
  if (item) {
    const err: any = new Error("Email already exist");
    err.statusCode = 409;
    throw err;
  }

  const token = crypto.randomBytes(32).toString("hex");
  signupRequests.set(data.email, token);

  const link = `${client}/confirm-email?token=${token}&email=${data.email}`;

  try {
    await sendEmail(
      data.email,
      "Confirmation",
      {
        link,
      },
      "./template/requestSignup.handlebars"
    );
  } catch (err) {
    console.error("Email service error");
  }

  return true;
};

export const confirmSignup = async (
  email: string,
  password: string,
  token: string
) => {
  console.log("--email, token, password", email, token, password);
  const signupRequest = signupRequests.get(email);
  console.log("--requ:", signupRequest);
  const isValid = signupRequest && token === signupRequest.token;

  if (!isValid) {
    const err: any = new Error("Email or token not valid");
    err.statusCode = 409;
    throw err;
  }

  const hash = await bcrypt.hash(password, Number(bcryptSalt));

  const user = {
    id: uuidv4(),
    email,
    score: 0,
    username: Math.random().toString(),
    password: hash,
  };

  console.log("--insert user:", user);

  const jwtToken = JWT.sign({ id: user.id }, JWTSecret);
  const result = await userActions.insertUser(user);
  console.log("--insert result:", result);

  try {
    await sendEmail(
      user.email,
      "Welcome",
      {
        name: user.username,
      },
      "./template/welcome.handlebars"
    );
  } catch (err) {
    console.error("Email service error");
  }

  return {
    score: user.score,
    userId: user.id,
    username: user.username,
    token: jwtToken,
  };
};

export const requestPasswordReset = async (username: any) => {
  console.log("request password reset", username);
  let user;
  if (username.includes("@")) {
    user = await userActions.findByEmail(username);
  } else {
    user = await userActions.findByUsername(username);
  }
  if (!user) {
    const err: any = new Error("User does not exist");
    err.statusCode = 422;
    throw err;
  }

  const token = await passwordResetTokenActions.findByUserId(user.id);
  if (token) await passwordResetTokenActions.deleteToken(token.token);

  const resetToken = crypto.randomBytes(32).toString("hex");
  const hash = await bcrypt.hash(resetToken, Number(bcryptSalt));

  await passwordResetTokenActions.insertToken({
    userId: user.id,
    token: hash,
    createdAt: Date.now(),
  });

  const link = `${client}/reset-password?token=${resetToken}&id=${user.id}`;

  try {
    await sendEmail(
      user.email,
      "Password Reset Request",
      {
        name: user.username,
        link,
      },
      "./template/requestResetPassword.handlebars"
    );
    return true;
  } catch (err) {
    console.log("services -> auth.service -> requestPasswordReset error:", err);
    throw new Error("Email service error");
  }
};

export const resetPassword = async (userId: any, token: any, password: any) => {
  console.log("reset password, user id:", userId);
  const passwordResetToken = await passwordResetTokenActions.findByUserId(
    userId
  );

  if (!passwordResetToken) {
    const err: any = new Error("Invalid or expired password reset token");
    err.statusCode = 400;
    throw err;
  }

  const isValid = await bcrypt.compare(token, passwordResetToken.token);

  if (!isValid) {
    const err: any = new Error("Invalid or expired password reset token");
    err.statusCode = 400;
    throw err;
  }

  const hash = await bcrypt.hash(password, Number(bcryptSalt));

  await userActions.updatePassword(userId, hash);

  const user = await userActions.findById(userId);
  await passwordResetTokenActions.deleteToken(passwordResetToken.token);

  if (!user) {
    console.error("resetPassword, no user found");
    throw new Error("Password reset email error, no user found");
  }

  try {
    await sendEmail(
      user.email,
      "Password Reset Successfully",
      {
        name: user.username,
      },
      "./template/resetPassword.handlebars"
    );
    return true;
  } catch (error) {
    throw new Error("Email service error");
  }
};
