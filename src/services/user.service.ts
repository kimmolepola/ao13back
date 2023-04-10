import { decode } from './auth.service';
import User from '../models/User.model';
import { getClients } from '../clients';

export const checkOkToStart = (token: any) => {
  const { id } = decode(token);
  if (getClients()[id]) {
    return { success: false, reason: 'Session already open with this user' };
  }
  return { success: true };
};

/* eslint-disable no-underscore-dangle, no-return-assign, no-param-reassign */
export const getUser = async (token: any) => {
  console.log('--getUser, token:', token);
  const { id } = decode(token);

  const user = await User.findOne({ _id: id });

  const data = user
    ? {
      score: user.score,
      userId: user._id,
      username: user.username,
      token,
    }
    : null;
  return data;
};

export const updateUsername = async (token: any, data: any) => {
  const { id } = decode(token);

  try {
    await User.updateOne(
      { _id: id },
      { $set: { username: data.username } },
      { new: true, runValidators: true, context: 'query' },
    );
  } catch (error) {
    throw new Error('Failed to update username');
  }

  const user: any = await User.findOne({ _id: id });

  return (data = {
    score: user.score,
    userId: user._id,
    username: user.username,
    token,
  });
};
