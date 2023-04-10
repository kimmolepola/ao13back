import {
  getTurnCredentials,
  login,
  signup,
  requestPasswordReset,
  resetPassword,
  logout,
} from '../services/auth.service';

export const getTokenFrom = (request: any) => {
  const authorization = request.get('authorization');
  if (authorization && authorization.toLowerCase().startsWith('bearer ')) {
    return authorization.substring(7);
  }
  return null;
};

export const logoutController = async (req: any, res: any, next: any) => {
  const token = getTokenFrom(req);
  const logoutService = await logout(token);
  return res.json(logoutService);
};

export const turnCredentialsController = async (req: any, res: any, next: any) => {
  const token = getTokenFrom(req);
  const turnCredentialsService = await getTurnCredentials(token);
  return res.json(turnCredentialsService);
};

export const loginController = async (req: any, res: any, next: any) => {
  const loginService = await login(req.body);
  return res.json(loginService);
};

export const signUpController = async (req: any, res: any, next: any) => {
  const signupService = await signup(req.body);
  return res.json(signupService);
};

export const resetPasswordRequestController = async (req: any, res: any, next: any) => {
  const requestPasswordResetService = await requestPasswordReset(
    req.body.username,
  );
  return res.json(requestPasswordResetService);
};

export const resetPasswordController = async (req: any, res: any, next: any) => {
  const resetPasswordService = await resetPassword(
    req.body.userId,
    req.body.token,
    req.body.password,
  );
  return res.json(resetPasswordService);
};
