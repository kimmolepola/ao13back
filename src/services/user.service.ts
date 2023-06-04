import { decode } from "./auth.service";
// import User from "../models/User.model";
import { getClients } from "../clients";
import * as userActions from "../db/user.actions";

export const checkOkToStart = (token: any) => {
  const { id } = decode(token);
  if (getClients()[id]) {
    return { success: false, reason: "Session already open with this user" };
  }
  return { success: true };
};

/* eslint-disable no-underscore-dangle, no-return-assign, no-param-reassign */
export const getUser = async (token: any) => {
  console.log("--getUser, token:", token);
  const { id } = decode(token);

  const user = await userActions.findById(id);
  // const user = await User.findOne({ _id: id });

  const data = user
    ? {
        score: user.score,
        userId: user.id,
        username: user.username,
        token,
      }
    : null;
  return data;
};

export const updateUsername = async (token: any, data: any) => {
  const { id } = decode(token);

  try {
    await userActions.udpateUsername(id, data.username);
  } catch (error) {
    console.error("updateUsername error:", error);
    throw new Error("Failed to update username");
  }

  const user = await userActions.findById(id);

  if (!user) {
    console.error("updateUsername user error");
    throw new Error("Failed to fetch user");
  }

  return (data = {
    score: user.score,
    userId: user.id,
    username: user.username,
    token,
  });
};
