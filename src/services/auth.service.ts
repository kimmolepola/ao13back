import 'dotenv/config';
import crypto from 'crypto';
import JWT from 'jsonwebtoken';
import bcrypt from 'bcrypt';
import User from '../models/User.model';
import Token from '../models/Token.model';
import sendEmail from '../utils/email/sendEmail';
import { disconnect } from '../index';

const JWTSecret = process.env.JWT_SECRET || '';
const bcryptSalt = process.env.BCRYPT_SALT;
const client = process.env.NODE_ENV === 'production'
  ? `https://${process.env.CLIENT_HOST}`
  : `http://${process.env.CLIENT_HOST}:${process.env.DEV_CLIENT_PORT}`;

export const decode = (token: any) => {
  const decodedToken: any = JWT.verify(token, JWTSecret);
  if (!token || !decodedToken.id) {
    const err: any = new Error('Invalid or missing token');
    err.statusCode = 401;
    throw err;
  }
  return decodedToken;
};

/* eslint-disable no-underscore-dangle, no-return-assign, no-param-reassign */
export const logout = async (token: any) => {
  console.log('--logout, token:', token);
  const { id } = decode(token);
  console.log('--logout, id:', id);
  disconnect(id);
  console.log(`logout ${id}`);
  return true;
};

export const getTurnCredentials = (token: any) => {
  console.log('--get TURN credentials', token);
  const { id } = decode(token);
  const secretKey = process.env.HMAC_SECRET;
  if (!secretKey) {
    const err: any = new Error('Server error');
    err.statusCode = 500;
    throw err;
  }

  // this credential will be valid for the next 60 seconds
  const unixTimeStamp = Math.floor(Date.now() / 1000) + 60;
  const username = [unixTimeStamp, id].join(':');
  const hmac = crypto.createHmac('sha1', secretKey);
  hmac.setEncoding('base64');
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
  console.log('login attempt:', data.username);
  let user;
  if (data.username.includes('@')) {
    user = await User.findOne({ email: data.username });
  } else {
    user = await User.findOne({ username: data.username });
  }
  const passwordCorrect = user === null
    ? false
    : await bcrypt.compare(data.password, user.password);

  if (!(user && passwordCorrect)) {
    const err: any = new Error('Invalid username, email or password');
    err.statusCode = 401;
    throw err;
  }

  const token = JWT.sign({ id: user._id }, JWTSecret);

  console.log('logged in:', user._id, user.username);

  return (data = {
    score: user.score,
    userId: user._id,
    username: user.username,
    token,
  });
};

export const signup = async (data: any) => {
  console.log('signup', data.email);
  let user = await User.findOne({ email: data.email });
  if (user) {
    const err: any = new Error('Email already exist');
    err.statusCode = 409;
    throw err;
  }
  data.score = 0;
  data.username = Math.random().toString();
  user = new User(data);

  const token = JWT.sign({ id: user._id }, JWTSecret);
  await user.save();

  try {
    await sendEmail(
      user.email,
      'Welcome',
      {
        name: user.username,
      },
      './template/welcome.handlebars',
    );
  } catch (err) {
    console.error('Email service error');
  }

  return (data = {
    score: user.score,
    userId: user._id,
    username: user.username,
    token,
  });
};

export const requestPasswordReset = async (username: any) => {
  console.log('request password reset', username);
  let user;
  if (username.includes('@')) {
    user = await User.findOne({ email: username });
  } else {
    user = await User.findOne({ username });
  }
  if (!user) {
    const err: any = new Error('User does not exist');
    err.statusCode = 422;
    throw err;
  }

  const token = await Token.findOne({ userId: user._id });
  if (token) await token.deleteOne();

  const resetToken = crypto.randomBytes(32).toString('hex');
  const hash = await bcrypt.hash(resetToken, Number(bcryptSalt));

  await new Token({
    userId: user._id,
    token: hash,
    createdAt: Date.now(),
  }).save();

  // const link = `https://${clientURL}/passwordreset?token=${resetToken}&id=${user._id}`;
  const link = `${client}/resetpassword?token=${resetToken}&id=${user._id}`;

  try {
    await sendEmail(
      user.email,
      'Password Reset Request',
      {
        name: user.username,
        link,
      },
      './template/requestResetPassword.handlebars',
    );
    return true;
  } catch (err) {
    console.log('services -> auth.service -> requestPasswordReset error:', err);
    throw new Error('Email service error');
  }
};

export const resetPassword = async (userId: any, token: any, password: any) => {
  console.log('reset password, user id:', userId);
  const passwordResetToken: any = await Token.findOne({ userId });

  if (!passwordResetToken) {
    const err: any = new Error('Invalid or expired password reset token');
    err.statusCode = 400;
    throw err;
  }

  const isValid = await bcrypt.compare(token, passwordResetToken.token);

  if (!isValid) {
    const err: any = new Error('Invalid or expired password reset token');
    err.statusCode = 400;
    throw err;
  }

  const hash = await bcrypt.hash(password, Number(bcryptSalt));

  await User.updateOne(
    { _id: userId },
    { $set: { password: hash } },
    { new: true },
  );

  const user: any = await User.findById({ _id: userId });

  await passwordResetToken.deleteOne();

  try {
    await sendEmail(
      user.email,
      'Password Reset Successfully',
      {
        name: user.username,
      },
      './template/resetPassword.handlebars',
    );
    return true;
  } catch (error) {
    throw new Error('Email service error');
  }
};
