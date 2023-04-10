import jsonwebtoken from 'jsonwebtoken';
import User from '../models/User.model';
import { getMain } from '../clients';

const JWTSecret = process.env.JWT_SECRET || '';

/* eslint-disable no-underscore-dangle, no-return-assign, no-param-reassign */
export const getGameObject = async (token: any, id: any) => {
  const decodedToken: any = token !== 'null' ? jsonwebtoken.verify(token, JWTSecret) : null;

  if (!token || !decodedToken || decodedToken.id !== getMain()) {
    const err: any = new Error('Unauthorized');
    err.statusCode = 401;
    throw err;
  }

  const user: any = await User.findOne({ _id: id });

  const data = {
    username: user.username,
    score: user.score,
    player: true,
  };
  return data;
};

export const saveGameState = async (token: string, data: { remoteId: string, score: number }[]) => {
  console.log('--saveGameState request, token', token);
  const decodedToken = jsonwebtoken.verify(token, JWTSecret) as any;
  console.log('--token id, main:', decodedToken, getMain());
  if (!token || !decodedToken.id || decodedToken.id !== getMain()) {
    const err: any = new Error('Unauthorized');
    err.statusCode = 401;
    throw err;
  }
  try {
    console.log('--try');
    const promises = data.map((x) => User.updateOne(
      { _id: x.remoteId },
      { $set: { score: x.score } },
      { new: true, runValidators: true, context: 'query' },
    ));
    const pro = await Promise.all(promises);
  } catch (error) {
    console.error(error);
    throw new Error('Failed to save game state');
  }
  return true;
};
