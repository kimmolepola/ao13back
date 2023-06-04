export type User = {
  id: string;
  email: string;
  username: string;
  password: string;
  score: number;
};

export type PasswordResetToken = {
  userId: string;
  token: string;
  createdAt: number;
};
