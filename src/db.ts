import mongoose from 'mongoose';

const { DB_URL } = process.env;

const connection = async () => {
  const conn = mongoose.connection;

  conn.on('connected', () => {
    console.log('connected to database');
  });

  conn.on('error', (err) => {
    console.log(`database connection error ${err}`);
  });

  conn.on('disconnected', () => {
    console.log('disconnected from database');
  });

  if (DB_URL) {
    try {
      console.log('connecting to database');
      await mongoose.connect(
        DB_URL,
        {
          useNewUrlParser: true,
          useUnifiedTopology: true,
          autoIndex: true,
        } as any,
      );
    } catch (error) {
      console.log(error);
    }
  } else {
    console.log('cannot connect to database, missing DB_URL');
  }
};

export default connection;
